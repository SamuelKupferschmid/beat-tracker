using BeatTracker.DFTPrototype.Utils;
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
        public const bool LOG = true;

        private readonly int _sampleRate;
        private readonly IDateTime _dateTime;

        private float _featureRate;

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

        private readonly Tempogram _tempogramCreator;

        private readonly PLPAggregator _aggregator;

        private DateTime? _startDateTime;

        private readonly SpectrumLogger _fftLogger = SpectrumLogger.Create("FFT");

        private readonly SpectrumLogger _noveltyLogger = SpectrumLogger.Create("Novelty");

        private readonly SpectrumLogger _tempogramLogger = SpectrumLogger.Create("Tempo");

        private readonly SpectrumLogger _plpLogger = SpectrumLogger.Create("PLP");

        public NaiveDftPipeline(int sampleRate, IDateTime dateTime)
        {
            _sampleRate = sampleRate;
            _dateTime = dateTime;

            var fftWindowSize = 1024;
            var fftStepSize = 512;

            _stftBuffer = new DataBuffer<float>(fftWindowSize, fftStepSize);
            _stftBuffer.NextFrame += StftBuffer_NextFrame;

            _smoothNormalizeWindow = NoveltyCurve.SmoothNormalizeFilter(_sampleRate, fftStepSize);
            _normalizeBuffer = new DataBuffer<float>(_smoothNormalizeWindow.Length);
            _normalizeBuffer.NextFrame += NormalizeBuffer_NextFrame;

            _smoothDiffWindow = NoveltyCurve.SmoothDifferentiateFilter(_sampleRate, fftStepSize);
            _diffBuffer = new DataBuffer<float>(_smoothDiffWindow.Length);
            _diffBuffer.NextFrame += SmoothDiffBuffer_NextFrame;

            _smoothLocalAverageWindow = NoveltyCurve.SmoothLocalAverageFilter(_sampleRate, fftStepSize);
            _localAverageBuffer = new DataBuffer<float>(_smoothLocalAverageWindow.Length);
            _localAverageBuffer.NextFrame += LocalAverageBuffer_NextFrame;

            _featureRate = (float)sampleRate / fftStepSize;

            var tempogramWindowLength = 4;
            var tempogramBufferSize = (int)Math.Ceiling(tempogramWindowLength * _featureRate);
            var tempogramStepSize = (int)Math.Ceiling(_featureRate / 5);

            _tempogramCreator = new Tempogram(tempogramBufferSize, tempogramStepSize, _featureRate , minBpm: 30, maxBpm: 600);

            _tempogramBuffer = new DataBuffer<float>(tempogramBufferSize, tempogramStepSize);
            _tempogramBuffer.NextFrame += TempogramBuffer_NextFrame;

            _aggregator = new PLPAggregator(tempogramBufferSize, tempogramStepSize);
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
            spec = NoveltyCurve.Preprocess(spec);

            //var amplifyToFreq = 6000f; // > 6000 Hz
            //var index = (int)Math.Ceiling((float)spec.Length / (_sampleRate / 2) * amplifyToFreq);

            //for (int i = index; i < spec.Length; i++)
            //    spec[i] *= 1.25f;

            if (LOG)
                _fftLogger.AddSampe(spec);

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

            _localAverageBuffer.Write(new[] { novelty }, 1);
        }

        private void LocalAverageBuffer_NextFrame(float[] data)
        {
            var novelty = data[data.Length / 2];

            NoveltyCurve.ApplyFilter(data, _smoothLocalAverageWindow, discardNegativeValues: true);
            var localAverage = data.Sum();

            novelty = Math.Max(novelty - localAverage, 0);

            // Console.WriteLine($"Novelty [{++noveltyCount}]: {novelty:F6}");

            //_noveltyCurve.Add(novelty);

            _tempogramBuffer.Write(new[] { novelty }, 1);

            if (LOG)
                _noveltyLogger.AddSampe(new[] { novelty });
        }

        private long tempogramCount = 0;

        private BeatInfo _currentBpm;
        private float _beatConfidenceThreshold = 0.85f;
        private float _bpmConfidenceThreshold = 0.10f;
        private float _similarityThreshold = 0.05f;

        private bool IsSimilarBpm(double bpm1, double bpm2)
        {
            bpm1 = Math.Round(bpm1 * 100) / 100;
            bpm2 = Math.Round(bpm2 * 100) / 100;

            var small = bpm1 < bpm2 ? bpm1 : bpm2;
            var big = bpm1 > bpm2 ? bpm1 : bpm2;

            while (big / 2 > small)
                big /= 2;

            return Math.Abs(small - big) / small < _similarityThreshold;
        }

        private void TempogramBuffer_NextFrame(float[] data)
        {
            var tempogram = _tempogramCreator.TempogramViaDFT(data);
            var bestBpm = _tempogramCreator.BestBpm(tempogram, _currentBpm);
            var plpCurve = _tempogramCreator.PLPCurve(tempogram, _currentBpm);

            if (LOG)
                _tempogramLogger.AddSampe(tempogram.Select(c => c.Magnitude).ToArray());

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

            //Console.WriteLine($"BPM (previous):{_currentBpm?.Bpm} | BPM (current):{bestBpm.Bpm}");
            if (_currentBpm == null
                || ((Math.Abs(_currentBpm.Confidence - bestBpm.Confidence) / _currentBpm.Confidence) > _bpmConfidenceThreshold
                    && !IsSimilarBpm(_currentBpm.Bpm, bestBpm.Bpm)))
            {
                Console.WriteLine($"BPM (previous):{_currentBpm?.Bpm} {_currentBpm?.Confidence:F4} | BPM (current):{bestBpm.Bpm} {bestBpm.Confidence:F4}");
                _currentBpm = bestBpm;
                // _aggregator.Clear();
            }

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

            for (int i = 0; i < _aggregator.BufferSize - 1; i++)
            {
                var current = _aggregator.ElementAt(i);
                var next = _aggregator.ElementAt((i + 1));

                if (current >= _beatConfidenceThreshold
                    && current > next)
                {
                    var bpm = _currentBpm.Bpm;
                    var offset = TimeSpan.FromSeconds((double)((_aggregator.TotalProcessed - _aggregator.StepSize + i) / _tempogramCreator.FeatureRate));
                    var occursAt = _startDateTime.Value + offset;

                    Console.WriteLine($"BPM:{bpm} | Confidence:{current}  | Occurs at:{occursAt} | Delay:{_dateTime.Now - occursAt}");
                    BeatIdentified?.Invoke(new BeatInfo(bpm, occursAt, current));
                }

                if (LOG)
                    _plpLogger.AddSampe(new[] { current });
            }
        }

        public event Action<BeatInfo> BeatIdentified;
    }
}
