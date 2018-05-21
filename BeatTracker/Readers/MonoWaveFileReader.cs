using System;
using System.IO;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Timer = System.Timers.Timer;

namespace BeatTracker.Readers
{
    public class MonoWaveFileReader : IWaveStreamReader, IDisposable
    {
        private readonly Stream _stream;
        private readonly WaveFileReader _reader;
        private readonly ISampleProvider _sampleProvider;
        private static readonly int _BufferSize = 1000;
        private bool _running = false;
        readonly float[] _buffer = new float[_BufferSize];

        public MonoWaveFileReader(Stream stream)
        {
            _stream = stream;
            _reader = new WaveFileReader(stream);
            if (_reader.WaveFormat.Channels == 2)
                _sampleProvider = new StereoToMonoProvider16(_reader).ToSampleProvider();
            else
                _sampleProvider = _reader.ToSampleProvider();
        }

        public MonoWaveFileReader(string filename)
            : this(File.OpenRead(filename))
        {
        }

        public void Start()
        {
            Start(false);
        }

        public void Start(bool simulatePlaybackspeed)
        {
            _running = true;
            if (simulatePlaybackspeed)
            {
                var timer = new Timer();

                timer.Elapsed += (sender, args) =>
                {
                    if (_running && _reader.Position < _reader.Length)
                    {
                        ReadNext();
                    }
                    else
                    {
                        timer.Stop();
                        timer.Dispose();
                    }
                };

                timer.AutoReset = true;

                if (WaveFormat.Channels == 2)
                    timer.Interval = 44100d / _BufferSize;
                else
                    timer.Interval = 22050d / _BufferSize;

                timer.Start();
            }
            else
            {
                while (_running && _reader.Position < _reader.Length)
                {
                    ReadNext();
                }
            }
        }

        private void ReadNext()
        {
            int length = _sampleProvider.Read(_buffer, 0, _BufferSize);

            OnDataAvailable(new WaveSamples(_buffer, length));
        }

        public void Stop()
        {
            _running = false;
        }

        public WaveFormat WaveFormat => _sampleProvider.WaveFormat;

        public event EventHandler<WaveSamples> DataAvailable;

        protected virtual void OnDataAvailable(WaveSamples e)
        {
            DataAvailable?.Invoke(this, e);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _reader?.Dispose();
        }
    }
}