using System;
using NAudio.Wave;

namespace BeatTracker.Readers
{
    public interface IWaveStreamReader
    {
        void Start();

        event EventHandler<WaveSample> DataAvailable;

        WaveFormat WaveFormat { get; }
    }

    public class WaveSample
    {
        public WaveSample(float[] data, int length)
        {
            Data = data;
            Length = length;
        }

        public float[] Data { get; }
        public int Length { get; }
    }
}