using System;
using System.IO;
using NAudio.Wave;

namespace BeatTracker.Readers
{
    public class MonoWaveFileReader : IWaveStreamReader, IDisposable
    {
        private readonly Stream _stream;
        private readonly WaveFileReader _reader;
        private readonly ISampleProvider _sampleProvider;
        

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
            const int size = 1000;

            float[] buffer = new float[size];
            
            while (_reader.Position < _reader.Length)
            {
                int length = _sampleProvider.Read(buffer, 0, size);

                OnDataAvailable(new WaveSample(buffer, length));
            }
        }

        private event EventHandler<WaveSample> DataAvailable;
        public WaveFormat WaveFormat => _reader.WaveFormat;

        event EventHandler<WaveSample> IWaveStreamReader.DataAvailable
        {
#pragma warning disable 4014
            add
            {
                this.DataAvailable += value;
            }
#pragma warning restore 4014
            remove { this.DataAvailable -= value; }
        }

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