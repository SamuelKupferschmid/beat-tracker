using BeatTracker.Readers;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Tracking
{
    public class BufferedTransformer
    {
        private int _stepBufferPos = 0;
        private float[] _stepBuffer;

        private float[] _windowBuffer;
        private int _totalProcessed;
        private readonly Func<float[], float[]> _transformerFunc;

        public BufferedTransformer(int windowSize, Func<float[], float[]> transformerFunc = null)
            : this(windowSize, windowSize, transformerFunc)
        {
        }

        public BufferedTransformer(int windowSize, int stepSize, Func<float[], float[]> transformerFunc = null)
        {
            if (windowSize <= 0)
                throw new ArgumentException(nameof(windowSize));

            if (stepSize <= 0 || stepSize > windowSize)
                throw new ArgumentException(nameof(stepSize));

            _transformerFunc = transformerFunc;

            WindowSize = windowSize;
            StepSize = stepSize;

            _stepBuffer = new float[stepSize];
            _windowBuffer = new float[windowSize];
        }

        public int WindowSize { get; }

        public int StepSize { get; }

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
                    var window = new float[WindowSize];
                    Array.Copy(_windowBuffer, _stepBuffer.Length, window, 0, WindowSize - _stepBuffer.Length);
                    Array.Copy(_stepBuffer, 0, window, WindowSize - _stepBuffer.Length, _stepBuffer.Length);

                    if (_totalProcessed >= WindowSize)
                    {
                        CopyAndTransform(window);
                    }

                    _windowBuffer = window;

                    _stepBuffer = new float[StepSize];
                    _stepBufferPos = 0;
                }

                processed += actual;
                remaining -= actual;

                if (_totalProcessed < WindowSize)
                    _totalProcessed += actual;
            }
        }

        protected void CopyAndTransform(float[] data)
        {
            var copy = new float[data.Length];
            Array.Copy(data, copy, data.Length);

            var transformed = _transformerFunc == null ? copy : _transformerFunc(copy);

            FrameAvailable?.Invoke(this, transformed);
        }

        public event EventHandler<float[]> FrameAvailable;
    }
}
