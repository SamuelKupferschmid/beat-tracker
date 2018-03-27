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

        public FFTransformer(int windowSize)
        {
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

        public void AddSample(WaveSample sample)
        {
            var sourcePos = 0;
            while (sourcePos < sample.Length)
            {
                var space = _buffer.Length - _bufferPosition;
                if (space > sample.Length - sourcePos)
                {
                    Array.Copy(sample.Data, 0, _buffer, _bufferPosition, sample.Length - sourcePos);
                    sourcePos += sample.Length;
                    _bufferPosition += sample.Length;
                }
                else
                {
                    Array.Copy(sample.Data, sourcePos, _buffer, _bufferPosition, space);
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

            for (var i = 0; i < data.Length; i++)
            {
                data[i] *= _equalizer[i];
            }
            FrameAvailable?.Invoke(this, data);
        }

        public event EventHandler<float[]> FrameAvailable;
    }
}