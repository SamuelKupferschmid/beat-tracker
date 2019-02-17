using System;

namespace BeatTracker.DFTPrototype.Utils
{
    public class DataBuffer<TData>
        where TData : struct
    {
        private readonly TData[] _buffer;
        private readonly int _stepSize;

        private int _bufferIndex = 0;
        private int _stepIndex = 0;

        private long _totalProcessed;

        public DataBuffer(int bufferSize)
            : this(bufferSize, 1)
        {
        }

        public DataBuffer(int bufferSize, int stepSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentException(nameof(bufferSize));

            if (stepSize <= 0 || stepSize > bufferSize)
                throw new ArgumentException(nameof(stepSize));

            _buffer = new TData[bufferSize];
            _stepSize = stepSize;
        }

        public int BufferSize => _buffer.Length;

        public int StepSize => _stepSize;

        public long TotalProcessed => _totalProcessed;

        public event Action<TData[]> NextFrame;

        public void Write(TData[] data, int length)
        {
            for (int i = 0; i < length; i++)
                Push(data[i]);
        }

        private void Push(TData item)
        {
            IncrementBufferIndex();
            IncrementStepIndex();

            _buffer[_bufferIndex] = item;

            ++_totalProcessed;
            
            if (_totalProcessed >= BufferSize
                && _stepIndex == 0)
            {
                var copy = new TData[BufferSize];

                var index = _bufferIndex + 1;
                
                if (index == BufferSize)
                    index = 0;

                for (int i = 0; i < BufferSize; i++)
                {
                    copy[i] = _buffer[index];
                    ++index;

                    if (index == BufferSize)
                        index = 0;
                }

                NextFrame?.Invoke(copy);
            }
        }

        private void IncrementBufferIndex()
        {
            ++_bufferIndex;

            if (_bufferIndex == BufferSize)
                _bufferIndex = 0;
        }

        private void IncrementStepIndex()
        {
            ++_stepIndex;

            if (_stepIndex == StepSize)
                _stepIndex = 0;
        }
    }
}
