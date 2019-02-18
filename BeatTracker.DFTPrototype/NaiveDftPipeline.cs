using BeatTracker.DFTPrototype.Utils;
using BeatTracker.Helpers;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using BeatTracker.Utils;
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

        private DataBuffer<float> _stftBuffer;

        private double[] _smoothNormalizeWindow;
        private DataBuffer<float> _normalizeBuffer;

        private double[] _smoothDiffWindow;
        private DataBuffer<float> _diffBuffer;
        
        private DataBuffer<float> _resampleBuffer;
        private float _resampleBufferLength;
        private float _resampleRate;

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

        private readonly PulseAnalyzer _pulseAnalyzer;

        public NaiveDftPipeline(int sampleRate, IDateTime dateTime)
        {
            _sampleRate = sampleRate;
            _dateTime = dateTime;

            var fftWindowSize = 1024;
            var fftStepSize = 512;

            _featureRate = (float)sampleRate / fftStepSize;

            _stftBuffer = new DataBuffer<float>(fftWindowSize, fftStepSize);
            _stftBuffer.NextFrame += StftBuffer_NextFrame;

            _smoothNormalizeWindow = NoveltyCurve.SmoothNormalizeFilter(_sampleRate, fftStepSize);
            _normalizeBuffer = new DataBuffer<float>(_smoothNormalizeWindow.Length);
            _normalizeBuffer.NextFrame += NormalizeBuffer_NextFrame;

            _smoothDiffWindow = NoveltyCurve.SmoothDifferentiateFilter(_sampleRate, fftStepSize);
            _diffBuffer = new DataBuffer<float>(_smoothDiffWindow.Length);
            _diffBuffer.NextFrame += SmoothDiffBuffer_NextFrame;

            // Performance von Resampling ist unbrauchbar:
            //_resampleBufferLength = 2;
            //_resampleRate = 200;
            //var resampleBufferSize = (int)Math.Ceiling(_resampleBufferLength * _featureRate);
            //_resampleBuffer = new DataBuffer<float>(resampleBufferSize, resampleBufferSize);
            //_resampleBuffer.NextFrame += ResampleBuffer_NextFrame;
            //_featureRate = _resampleRate;

            _smoothLocalAverageWindow = NoveltyCurve.SmoothLocalAverageFilter((int)Math.Ceiling(_featureRate));
            _localAverageBuffer = new DataBuffer<float>(_smoothLocalAverageWindow.Length);
            _localAverageBuffer.NextFrame += LocalAverageBuffer_NextFrame;

            var tempogramWindowLength = 6;
            var tempogramBufferSize = (int)Math.Ceiling(tempogramWindowLength * _featureRate);
            //var tempogramStepSize = (int)Math.Ceiling(_featureRate / 5);
            var tempogramStepSize = (int)Math.Ceiling(_featureRate / 10);

            _tempogramUtil = new Tempogram(tempogramBufferSize, tempogramStepSize, _featureRate, minBpm: 30, maxBpm: 600);

            _tempogramBuffer = new DataBuffer<float>(tempogramBufferSize, tempogramStepSize);
            _tempogramBuffer.NextFrame += TempogramBuffer_NextFrame;

            _aggregator = new PLPAggregator(tempogramBufferSize, tempogramStepSize);

            _slidingBpmConfidence = new ConcurrentSlidingEnumerable<double>(tempogramBufferSize / 2);

            //_pulseAnalyzer = new PulseAnalyzer();
        }

        public void Write(float[] data, int length)
        {
            if (!_startDateTime.HasValue)
                _startDateTime = _dateTime.Now;

            _stftBuffer.Write(data, length);
        }

        private long stftCount = 0;

        private void StftBuffer_NextFrame(float[] data)
        {
            // Console.WriteLine($"STFT [{++stftCount}]");

            var spec = Spectrogram.ToMagnitudeSpectrogram(data);

            spec = NoveltyCurve.Preprocess(_sampleRate, spec);

            //var amplifyToFreq = 6000f; // > 6000 Hz
            //var index = (int)Math.Ceiling((float)spec.Length / (_sampleRate / 2) * amplifyToFreq);

            //for (int i = index; i < spec.Length; i++)
            //    spec[i] *= 1.25f;

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

        private long normalizeCount = 0;

        private void NormalizeBuffer_NextFrame(float[] data)
        {
            NoveltyCurve.ApplyFilter(data, _smoothNormalizeWindow, discardNegativeValues: false);
            _normalizeFactor = data.Sum();
                        
            // Console.WriteLine($"Normalize Value [{++normalizeCount}]: {_normalizeFactor:F6}");
        }

        private long noveltyCount = 0;

        private void SmoothDiffBuffer_NextFrame(float[] data)
        {
            NoveltyCurve.ApplyFilter(data, _smoothDiffWindow, discardNegativeValues: true);
            var novelty = data.Sum() / _normalizeFactor.Value;

            if (_resampleBuffer != null)
                _resampleBuffer.Write(new[] { novelty }, 1);
            else
               _localAverageBuffer.Write(new[] { novelty }, 1);
        }

        private void ResampleBuffer_NextFrame(float[] data)
        {
            var sampleSize = (int)Math.Ceiling(_resampleBufferLength * _resampleRate);
            var resampledData = NoveltyCurve.Resample(data, sampleSize);

            _localAverageBuffer.Write(resampledData, resampledData.Length);
        }

        private void LocalAverageBuffer_NextFrame(float[] data)
        {
            var novelty = data[data.Length / 2];

            NoveltyCurve.ApplyFilter(data, _smoothLocalAverageWindow, discardNegativeValues: true);
            var localAverage = data.Sum();

            novelty = Math.Max(novelty - localAverage, 0);

            //Console.WriteLine($"Novelty [{++noveltyCount}]: {novelty:F6}");

            _noveltyCurve.Add(novelty);

            _tempogramBuffer.Write(new[] { novelty }, 1);

            if (LogNoveltyCurve)
            {
                var log = new float[300];
                log[(int)Math.Floor(novelty / 10 * log.Length).Clamp(0, log.Length - 1)] = 1;
                _noveltyLogger.AddSampe(log);
            }

            //_pulseAnalyzer.AddFrame(novelty);
        }

        private long tempogramCount = 0;

        private BeatInfo _currentBpm;
        private float _beatConfidenceThreshold = 0.75f;
        private int _bpmConfidenceSize = 25;

        private float _similarityThreshold = 0.025f;

        private bool IsSimilarBpm(double bpm1, double bpm2)
        {
            return false;

            bpm1 = Math.Round(bpm1 * 100) / 100;
            bpm2 = Math.Round(bpm2 * 100) / 100;

            var small = bpm1 < bpm2 ? bpm1 : bpm2;
            var big = bpm1 > bpm2 ? bpm1 : bpm2;

            while (small > 90)
                small /= 2;

            while (big / 2 > small)
                big /= 2;

            return Math.Abs(small - big) / small < _similarityThreshold;
        }

        private ConcurrentSlidingEnumerable<double> _slidingBpmConfidence;

        private void TempogramBuffer_NextFrame(float[] data)
        {
            var tempogram = _tempogramUtil.TempogramViaDFT(data);
            var bestBpm = _tempogramUtil.BestBpm(tempogram, null);
            var plpCurve = _tempogramUtil.PLPCurve(tempogram, null);

            if (LogTempogram)
            {
                var maxMagnitude = tempogram.Max(c => c.Magnitude);
                _tempogramLogger.AddSampe(tempogram.Select(c => c.Magnitude / maxMagnitude).ToArray());
            }

            // ToDo - Confidence Schwellwert automatisch anpassen
            // ToDo - hervorheben der Freq-Bänder oder bandwise processing?

            //Console.WriteLine($"BPM (previous):{_currentBpm?.Bpm} | BPM (current):{bestBpm.Bpm}");
            //_currentBpm = bestBpm;

            //for (int i = 0; i < plpCurve.Length - 1; i++)
            //{
            //    var current = plpCurve[i];
            //    var next = plpCurve[i + 1];

            //    if (current >= _beatConfidenceThreshold
            //        && current > next)
            //    {
            //        var bpm = _currentBpm.Bpm;
            //        var offset = TimeSpan.FromSeconds((double)((_tempogramBuffer.TotalProcessed - _aggregator.StepSize + i) / _tempogramCreator.FeatureRate));
            //        var occursAt = _startDateTime.Value + offset;

            //        Console.WriteLine($"BPM:{bpm} | Confidence:{current}  | Occurs at:{occursAt} | Delay:{_dateTime.Now - occursAt}");
            //        BeatIdentified?.Invoke(new BeatInfo(bpm, occursAt, current));

            //        // peaks.Add((i, current, offset));
            //    }
            //}


            if (_currentBpm == null
                || (Math.Abs(_currentBpm.Bpm - bestBpm.Bpm) > 2.5f)
                    && bestBpm.Confidence >= _slidingBpmConfidence.Where(c => c > 0).Average())
                //|| ((Math.Abs(_currentBpm.Bpm - bestBpm.Bpm) > 0.5f)
                //    && bestBpm.Confidence >= _slidingBpmConfidence.Where(c => c > 0).Average()))
            {
                // Console.WriteLine($"BPM (previous):{_currentBpm?.Bpm} {_currentBpm?.Confidence:F4} | BPM (current):{bestBpm.Bpm} {bestBpm.Confidence:F4}");
                _currentBpm = bestBpm;
                //_aggregator.Clear();

                for(int i = 0; i < _bpmConfidenceSize; i++)
                    _slidingBpmConfidence.Push(bestBpm.Confidence);
            }
            
            _slidingBpmConfidence.Push(tempogram.Select(c => c.Magnitude).OrderByDescending(f => f).Take(_bpmConfidenceSize).Average());

            //Console.WriteLine($"BPM (previous):{_currentBpm?.Bpm} | BPM (current):{bestBpm.Bpm}");
            //if (_currentBpm == null
            //    || ((Math.Abs(_currentBpm.Confidence - bestBpm.Confidence) / _currentBpm.Confidence) > _bpmConfidenceThreshold
            //        && !IsSimilarBpm(_currentBpm.Bpm, bestBpm.Bpm)))
            //{
            //    Console.WriteLine($"BPM (previous):{_currentBpm?.Bpm} {_currentBpm?.Confidence:F4} | BPM (current):{bestBpm.Bpm} {bestBpm.Confidence:F4}");
            //    _currentBpm = bestBpm;

            //    //_beatConfidenceThreshold = _aggregator.Mean;

            //    //Console.WriteLine($"Beat confidence threshold: {_beatConfidenceThreshold:F4}");

            //    //_aggregator.Clear();
            //}

            //if (_currentBpm == null
            //    || (Math.Abs(_currentBpm.Confidence - bestBpm.Confidence) / _currentBpm.Confidence) >= _bpmConfidenceChangeThreshold)
            ////|| ((bestBpm.Confidence > _currentBpm.Confidence) && !IsSimilarBpm(_currentBpm.Bpm, bestBpm.Bpm)))
            ////|| (bestBpm.Confidence > _currentBpm.Confidence)
            ////|| (Math.Abs(_currentBpm.Confidence - bestBpm.Confidence) / _currentBpm.Confidence) >= _bpmConfidenceChangeThreshold)
            //{
            //    Console.WriteLine($"BPM (previous):{_currentBpm?.Bpm} | BPM (current):{bestBpm.Bpm}");

            //    _currentBpm = bestBpm;
            //    _aggregator.Clear();
            //}

            // Console.WriteLine($"Tempogram [{++tempogramCount}]");

            //Console.WriteLine($"BPM (previous):{_currentBpm?.Bpm} | BPM (current):{bestBpm.Bpm}");

            //_currentBpm = bestBpm;

            _aggregator.Aggregate(plpCurve, plpCurve.Length);
            _beatConfidenceThreshold = _aggregator.Mean;

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
                        var offset = TimeSpan.FromSeconds((double)((_aggregator.TotalProcessed - _aggregator.StepSize + i) / _tempogramUtil.FeatureRate));
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
