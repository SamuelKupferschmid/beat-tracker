using System;
using NAudio.Wave;

namespace BeatTracker.Readers
{
    public interface IWaveStream
    {
        event EventHandler<SampleArgs> DataAvailable;

        WaveFormat WaveFormat { get; }
    }

    public class SampleArgs
    {
        public SampleArgs(float[] data, int length)
        {
            Data = data;
            Length = length;
        }

        public float[] Data { get; }
        public int Length { get; }
    }
}
