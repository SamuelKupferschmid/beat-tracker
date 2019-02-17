using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.DFTPrototype.Utils
{
    public class PLPAggregator
    {
        private readonly int _bufferSize;
        private readonly int _stepSize;

        private readonly float[] _buffer;
        private readonly float[,] _stepBuffer;
        private int _stepIndex = 0;

        private long _totalProcessed;

        public PLPAggregator(int bufferSize, int stepSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentException(nameof(bufferSize));

            if (stepSize <= 0 || stepSize > bufferSize)
                throw new ArgumentException(nameof(stepSize));

            _bufferSize = bufferSize;
            _stepSize = stepSize;

            _buffer = new float[bufferSize];
            var steps = (int)Math.Ceiling((float)bufferSize / stepSize);
            _stepBuffer = new float[steps, bufferSize];
        }

        public int BufferSize => _bufferSize;

        public int StepSize => _stepSize;

        public int StepIndex => _stepIndex;

        public int StepCount => _stepBuffer.GetLength(0);

        public long TotalProcessed => _totalProcessed;

        public float ElementAt(int index) => _buffer[index];

        public void Aggregate(float[] data, int length)
        {
            if (length != BufferSize)
                throw new ArgumentException("Length must be equal to BufferSize");

            IncrementStepIndex();

            for (int i = 0; i < length; i++)
                _stepBuffer[_stepIndex, i] = data[i];

            UpdateInternalBuffer();

            _totalProcessed += StepSize;
        }

        public void Clear()
        {
            for (int i = 0; i < StepCount; i++)
                for (int j = 0; j < BufferSize; j++)
                _stepBuffer[i, j] = 0;

            for (int i = 0; i < BufferSize; i++)
                _buffer[i] = 0;
        }

        private void IncrementStepIndex()
        {
            ++_stepIndex;

            if (_stepIndex == StepCount - 1)
                _stepIndex = 0;
        }

        private void UpdateInternalBuffer()
        {
            Array.Clear(_buffer, 0, _buffer.Length);

            var stepIndex = _stepIndex;
            for (int i = 0; i < StepCount; i++)
            {
                for (int j = i; j < BufferSize; j++)
                {
                    _buffer[j - i] += _stepBuffer[stepIndex, j];
                }

                --stepIndex;
                if (stepIndex < 0)
                    stepIndex = StepCount - 1;
            }
        }
    }
}
