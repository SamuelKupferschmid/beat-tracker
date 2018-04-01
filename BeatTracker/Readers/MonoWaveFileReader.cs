using System;
using System.IO;
using System.Threading;
using NAudio.Wave;
using Timer = System.Timers.Timer;

namespace BeatTracker.Readers
{
    public class MonoWaveFileReader : IWaveStreamReader, IDisposable
    {
        private readonly Stream _stream;
        private readonly WaveFileReader _reader;
        private readonly ISampleProvider _sampleProvider;
        private static readonly int _BufferSize = 1000;
        private bool running = false;
        readonly float[] _buffer = new float[_BufferSize];

        public MonoWaveFileReader(Stream stream)
        {
            _stream = stream;
            _reader = new WaveFileReader(stream);
            _sampleProvider = new StereoToMonoProvider16(_reader).ToSampleProvider();
        }

        public MonoWaveFileReader(string filename)
            : this(File.OpenRead(filename))
        {
        }

        public void Start()
        {
            Start(true);
        }

        public void Start(bool simulatePlaybackspeed)
        {
            running = true;
            if (simulatePlaybackspeed)
            {
                var timer = new Timer();

                timer.Elapsed += (sender, args) =>
                {
                    if (running && _reader.Position < _reader.Length)
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
                timer.Interval = 44100d / _BufferSize;
                timer.Start();
            }
            else
            {
                while (running && _reader.Position < _reader.Length)
                {
                    ReadNext();
                }
            }
        }

        private void ReadNext()
        {
            int length = _sampleProvider.Read(_buffer, 0, _BufferSize);
            OnDataAvailable(new WaveSample(_buffer, length));
        }

        public void Stop()
        {
            running = false;
        }

        public WaveFormat WaveFormat => _reader.WaveFormat;

        public event EventHandler<WaveSample> DataAvailable;

        protected virtual void OnDataAvailable(WaveSample e)
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