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
        private float[] _buffer;
        private int _bufferPosition;
        private Func<int, float> _equalizerFunc;

        private int _windowSize;
        private int _stepSize;
        private int _coefficientRangeMin;
        private int _coefficientRangeMax;
        
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
            _buffer = new float[windowSize];
            _bufferPosition = 0;

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
            var sourcePos = 0;
            while (sourcePos < samples.Length)
            {
                var chunk = new float[_windowSize];
                var length = Math.Min(_windowSize, samples.Length - sourcePos);
                Array.Copy(samples.Data, sourcePos, chunk, 0, length);
                sourcePos += length;

                AddChunk(chunk);
            }
        }
        
        private void AddChunk(float[] chunk)
        {
            var sourcePos = 0;
            while (sourcePos < chunk.Length)
            {
                var space = _buffer.Length - _bufferPosition;
                if (space > chunk.Length - sourcePos)
                {
                    Array.Copy(chunk, 0, _buffer, _bufferPosition, chunk.Length - sourcePos);
                    sourcePos += chunk.Length;
                    _bufferPosition += chunk.Length;
                }
                else
                {
                    Array.Copy(chunk, sourcePos, _buffer, _bufferPosition, space);
                    Tranform(_buffer);

                    _buffer = new float[_bins * 2];
                    _bufferPosition = 0;
                    sourcePos += space;
                }
            }
        }
               

        protected void Tranform(float[] data)
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