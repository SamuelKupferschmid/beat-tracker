using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using MathNet.Numerics.IntegralTransforms;

namespace BeatTracker.Tracking
{
    public class FFTransformer
    {
        private readonly int _bins;
        private float[] _equalizer;

        private int _stepBufferPos = 0;
        private float[] _stepBuffer;

        private float[] _windowBuffer;

        private Func<int, float> _equalizerFunc;

        private int _windowSize;
        private int _stepSize;
        private int _coefficientRangeMin;
        private int _coefficientRangeMax;

        private int _totalProcessed;

        public FFTransformer(int windowSize)
            : this(windowSize, windowSize)
        {
        }

        public FFTransformer(int windowSize, int stepSize)
            : this(windowSize, stepSize, 0, (windowSize / 2) + 1)
        {
        }

        public FFTransformer(int windowSize, int stepSize, int coefficientRangeMin, int coefficientRangeMax)
        {
            if (windowSize <= 0)
                throw new ArgumentException(nameof(windowSize));

            if (stepSize <= 0 || stepSize > windowSize)
                throw new ArgumentException(nameof(stepSize));

            if (coefficientRangeMin < 0)
                throw new ArgumentException(nameof(coefficientRangeMin));

            if (coefficientRangeMax < 0 || coefficientRangeMax < coefficientRangeMin)
                throw new ArgumentException(nameof(coefficientRangeMax));

            _windowSize = windowSize;
            _stepSize = stepSize;
            _coefficientRangeMin = coefficientRangeMin;
            _coefficientRangeMax = coefficientRangeMax;
            
            _bins = windowSize / 2;

            _stepBuffer = new float[stepSize];
            _windowBuffer = new float[windowSize];

            _equalizer = Enumerable.Repeat(1f, windowSize).ToArray();
        }

        public Func<int, float> EqualizerFunc
        {
            get => _equalizerFunc;
            set
            {
                _equalizerFunc = value;
                _equalizer = Enumerable.Range(0, _bins * 2).Select(_equalizerFunc).ToArray();
            }
        }

        public void AddSamples(WaveSamples samples)
        {
            Push(samples.Data, samples.Length);
        }

        private void Push(float[] data, int length)
        {
            var processed = 0;
            var remaining = length;

            while (remaining > 0)
            {
                var available = _stepBuffer.Length - _stepBufferPos;
                var actual = Math.Min(remaining, available);

                if (actual > 0)
                {
                    Array.Copy(data, 0, _stepBuffer, _stepBufferPos, actual);
                    _stepBufferPos += actual;
                }
                else if (_stepBufferPos == _stepBuffer.Length)
                {
                    var window = new float[_windowSize];
                    Array.Copy(_stepBuffer, window, _stepBuffer.Length);
                    Array.Copy(_windowBuffer, 0, window, _stepBuffer.Length, _windowSize - _stepBuffer.Length);

                    if (_totalProcessed >= _windowSize)
                    {
                        Transform(window);
                    }

                    _windowBuffer = window;

                    _stepBuffer = new float[_stepSize];
                    _stepBufferPos = 0;
                }

                processed += actual;
                remaining -= actual;

                if (_totalProcessed < _windowSize)
                    _totalProcessed += actual;
            }
        }

        protected void Transform(float[] data)
        {
            Fourier.ForwardReal(data, data.Length - 1);
            
            if (_equalizerFunc != null)
            {
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] *= _equalizer[i];
                }
            }

            int range = _coefficientRangeMax - _coefficientRangeMin;
            if (range < data.Length)
            {
                var rangeData = new float[range];
                Array.Copy(data, _coefficientRangeMin, rangeData, 0, range);
                data = rangeData;
            }

            FrameAvailable?.Invoke(this, data);
        }

        public event EventHandler<float[]> FrameAvailable;
    }
}