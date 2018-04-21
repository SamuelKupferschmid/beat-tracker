using System;
using NAudio.Wave;

namespace BeatTracker.Readers
{
    public interface IWaveStreamReader
    {
        void Start();

        void Stop();

        event EventHandler<WaveSamples> DataAvailable;

        WaveFormat WaveFormat { get; }
    }

    public class WaveSamples
    {
        public WaveSamples(float[] data, int length)
        {
            Data = data;
            Length = length;
        }

        public float[] Data { get; }
        public int Length { get; }
    }
}