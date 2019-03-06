using BeatTracker.DFTPrototype.Utils;
using BeatTracker.Helpers;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using BeatTracker.Utils;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatTracker.DFTPrototype
{
    public class NaiveDftPipeline
    {
        public const bool LogFFT = true;
        public const bool LogNoveltyCurve = true;
        public const bool LogTempogram = true;
        public const bool LogPLPCurve = false;

        private readonly int _sampleRate;
        private readonly IDateTime _dateTime;

        private float _featureRate;

        private double[] _stftWindow;
        private DataBuffer<float> _stftBuffer;

        private double[] _smoothNormalizeWindow;
        private DataBuffer<float> _normalizeBuffer;

        private double[] _smoothDiffWindow;
        private DataBuffer<float> _diffBuffer;

        private double[] _smoothLocalAverageWindow;
        private DataBuffer<float> _localAverageBuffer;
        private DataBuffer<float> _tempogramBuffer;

        private float? _normalizeFactor = null;

        public readonly List<float> _noveltyCurve = new List<float>();

        public readonly List<float> _plpCurve = new List<float>();

        private readonly Tempogram _tempogramUtil;

        private readonly PLPAggregator _aggregator;

        private DateTime? _startDateTime;

        private readonly SpectrumLogger _fftLogger = LogFFT ? SpectrumLogger.Create("FFT") : null;

        private readonly SpectrumLogger _noveltyLogger = LogNoveltyCurve ? SpectrumLogger.Create("Novelty") : null;

        private readonly SpectrumLogger _tempogramLogger = LogTempogram ? SpectrumLogger.Create("Tempo") : null;

        private readonly SpectrumLogger _plpLogger = LogPLPCurve ? SpectrumLogger.Create("PLP") : null;

        public NaiveDftPipeline(int sampleRate, IDateTime dateTime)
        {
            _sampleRate = sampleRate;
            _dateTime = dateTime;

            var fftWindowSize = 1024;
            var fftStepSize = 1024;

            _featureRate = (float)sampleRate / fftStepSize;

            _stftWindow = Window.Hann(fftWindowSize);
            _stftBuffer = new DataBuffer<float>(fftWindowSize, fftStepSize);
            _stftBuffer.NextFrame += StftBuffer_NextFrame;

            _smoothNormalizeWindow = NoveltyCurve.SmoothNormalizeFilter(_sampleRate, fftStepSize);
            _normalizeBuffer = new DataBuffer<float>(_smoothNormalizeWindow.Length);
            _normalizeBuffer.NextFrame += NormalizeBuffer_NextFrame;

            _smoothDiffWindow = NoveltyCurve.SmoothDifferentiateFilter(_sampleRate, fftStepSize);
            _diffBuffer = new DataBuffer<float>(_smoothDiffWindow.Length);
            _diffBuffer.NextFrame += SmoothDiffBuffer_NextFrame;

            _smoothLocalAverageWindow = NoveltyCurve.SmoothLocalAverageFilter((int)Math.Ceiling(_featureRate));
            _localAverageBuffer = new DataBuffer<float>(_smoothLocalAverageWindow.Length);
            _localAverageBuffer.NextFrame += LocalAverageBuffer_NextFrame;

            var tempogramWindowLength = 8;
            var tempogramBufferSize = (int)Math.Ceiling(tempogramWindowLength * _featureRate);
            var tempogramStepSize = (int)Math.Ceiling(_featureRate / 10);

            _tempogramUtil = new Tempogram(tempogramBufferSize, tempogramStepSize, _featureRate, minBpm: 30, maxBpm: 600);

            _tempogramBuffer = new DataBuffer<float>(tempogramBufferSize, tempogramStepSize);
            _tempogramBuffer.NextFrame += TempogramBuffer_NextFrame;

            _aggregator = new PLPAggregator(tempogramBufferSize, tempogramStepSize);

            _slidingBpmConfidenceBuffer = new ConcurrentSlidingEnumerable<double>(tempogramBufferSize / 2);

            _normalizeFactor = 1;
        }

        public void Write(float[] data, int length)
        {
            if (!_startDateTime.HasValue)
                _startDateTime = _dateTime.Now;

            _stftBuffer.Write(data, length);
        }

        private void StftBuffer_NextFrame(float[] data)
        {
            var spec = Spectrogram.ToMagnitudeSpectrogram(data, _stftWindow);
            spec = NoveltyCurve.Preprocess(_sampleRate, spec);

            if (LogFFT)
            {
                _fftLogger.AddSampe(spec);
            }

            _normalizeBuffer.Write(new[] { spec.Sum() }, 1);

            if (_normalizeFactor.HasValue)
            {
                _diffBuffer.Write(new[] { spec.Sum() }, 1);
            }
        }

        private void NormalizeBuffer_NextFrame(float[] data)
        {
            NoveltyCurve.ApplyFilter(data, _smoothNormalizeWindow, discardNegativeValues: false);
            //_normalizeFactor = data.Sum();
            _normalizeFactor = 1;
        }

        private void SmoothDiffBuffer_NextFrame(float[] data)
        {
            NoveltyCurve.ApplyFilter(data, _smoothDiffWindow, discardNegativeValues: true);
            var novelty = data.Sum() / _normalizeFactor.Value;

            _localAverageBuffer.Write(new[] { novelty }, 1);
        }

        private void LocalAverageBuffer_NextFrame(float[] data)
        {
            var novelty = data[data.Length / 2];

            NoveltyCurve.ApplyFilter(data, _smoothLocalAverageWindow, discardNegativeValues: true);
            var localAverage = data.Sum();

            novelty = Math.Max(novelty - localAverage, 0);

            _noveltyCurve.Add(novelty);

            _tempogramBuffer.Write(new[] { novelty }, 1);

            if (LogNoveltyCurve)
            {
                var log = new float[300];
                log[(int)Math.Floor(novelty / 10 * log.Length).Clamp(0, log.Length - 1)] = 1;
                _noveltyLogger.AddSampe(log);
            }
        }

        private BeatInfo _currentBpm;
        private float _beatConfidenceThreshold = 0f;
        private int _bpmConfidenceSize = 10;

        private ConcurrentSlidingEnumerable<double> _slidingBpmConfidenceBuffer;

        private void TempogramBuffer_NextFrame(float[] data)
        {
            var tempogram = _tempogramUtil.TempogramViaDFT(data);
            var bestBpm = _tempogramUtil.BestBpm(tempogram);
            var plpCurve = _tempogramUtil.PLPCurve(tempogram);

            if (LogTempogram)
            {
                var maxMagnitude = tempogram.Max(c => c.Magnitude);
                _tempogramLogger.AddSampe(tempogram.Select(c => c.Magnitude / maxMagnitude).ToArray());
            }                       
            
            if (_currentBpm == null
                || (Math.Abs(_currentBpm.Bpm - bestBpm.Bpm) > 0.5f))
                    //&& bestBpm.Confidence >= _slidingBpmConfidenceBuffer.Where(c => c > 0).Average())
            {
                // Console.WriteLine($"BPM (previous):{_currentBpm?.Bpm} {_currentBpm?.Confidence:F4} | BPM (current):{bestBpm.Bpm} {bestBpm.Confidence:F4}");
                _currentBpm = bestBpm;

                //for (int i = 0; i < _bpmConfidenceSize; i++)
                //    _slidingBpmConfidenceBuffer.Push(bestBpm.Confidence);
            }
            
            //_slidingBpmConfidenceBuffer.Push(tempogram.Select(c => c.Magnitude).OrderByDescending(f => f).Take(_bpmConfidenceSize).Average());
            
            _aggregator.Aggregate(plpCurve, plpCurve.Length);
            _beatConfidenceThreshold = _aggregator.Average();

            for (int i = 0; i < _aggregator.BufferSize - 1; i++)
            {
                var current = _aggregator.ElementAt(i);
                var next = _aggregator.ElementAt((i + 1));

                if (current >= _beatConfidenceThreshold
                    && current > next)
                {
                    if (current > next)
                    {
                        var bpm = _currentBpm.Bpm;
                        var offset = TimeSpan.FromSeconds((_aggregator.TotalProcessed - _aggregator.StepSize + i) / _tempogramUtil.FeatureRate);
                        var occursAt = _startDateTime.Value + offset;

                        //Console.WriteLine($"BPM:{bpm} | Confidence:{current}  | Occurs at:{occursAt} | Delay:{_dateTime.Now - occursAt}");
                        BeatIdentified?.Invoke(new BeatInfo(bpm, occursAt, current));
                    }

                    if (LogPLPCurve)
                    {
                        var log = new float[300];
                        log[(int)Math.Floor(current * log.Length).Clamp(0, log.Length - 1)] = 1;
                        _plpLogger.AddSampe(log);
                    }
                }
            }
        }

        public event Action<BeatInfo> BeatIdentified;
    }
}
