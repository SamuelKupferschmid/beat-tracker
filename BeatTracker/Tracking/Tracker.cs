using System;
using System.ComponentModel;
using System.Linq;
using BeatTracker.Readers;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Random;
using NAudio.Utils;
using NAudio.Wave;

namespace BeatTracker.Tracking
{
    public partial class Tracker : IDisposable
    {
        private readonly IWaveStream _source;
        private WaveFileWriter _writer;


        public Tracker(IWaveStream source)
        {
            _writer = new WaveFileWriter(@"C:\tmp\fft.wav", new WaveFormat(44100, 1));

            _source = source;
        }

        public event EventHandler<BeatInfo> BeatInfoChanged;

        public void Start()
        {
            this._source.DataAvailable += DataAvailable;
        }

        private void DataAvailable(object sender, SampleArgs e)
        {
            Fourier.Forward(e.Data, new float[e.Data.Length]);
            foreach (var f in e.Data)
            {
                _writer.WriteSample(f);
            }
        }

        public void Dispose()
        {
            this._source.DataAvailable -= DataAvailable;
            _writer.Flush();
            _writer.Dispose();
        }

        protected virtual void OnBeatInfoChanged(BeatInfo e)
        {
            BeatInfoChanged?.Invoke(this, e);
        }
    }
}