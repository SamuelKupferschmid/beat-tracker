using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace BeatTracker.Readers
{
    public class WaveInputDeviceReader : IWaveStreamReader, IDisposable
    {
        private static readonly int SampleBufferSize = 100;
        
        private readonly WaveIn _device;
        private readonly float[] _sampleBuffer;
        private readonly BufferedWaveProvider _bufferedWaveProvider;
        private readonly ISampleProvider _sampleProvider;


        public WaveInputDeviceReader(int deviceId, WaveFormat waveFormat)
        {
            if (deviceId < 0 || deviceId > WaveIn.DeviceCount - 1)
                throw new ArgumentException(nameof(deviceId));

            _device = new WaveIn();
            _device.DeviceNumber = deviceId;
            _device.WaveFormat = waveFormat ?? throw new ArgumentNullException(nameof(waveFormat));

            if (waveFormat.BitsPerSample == 0
                || waveFormat.BitsPerSample % 8 != 0)
                throw new ArgumentException("BitsPerSample must be greater than 0 and a factor of 8");

            _sampleBuffer = new float[SampleBufferSize];

            _bufferedWaveProvider = new BufferedWaveProvider(_device.WaveFormat);
            _bufferedWaveProvider.BufferLength = SampleBufferSize * waveFormat.BitsPerSample / 8;
            _bufferedWaveProvider.DiscardOnBufferOverflow = false;
            
            _sampleProvider = _bufferedWaveProvider.ToSampleProvider();
            
            _device.DataAvailable += _device_DataAvailable;
        }

        private void _device_DataAvailable(object sender, WaveInEventArgs e)
        {
            int bytesProcessed = 0;
            int bytesToProcess = e.BytesRecorded;

            while(bytesToProcess > 0)
            {
                int available = _bufferedWaveProvider.BufferLength - _bufferedWaveProvider.BufferedBytes;
                int length = bytesToProcess > available ? available : bytesToProcess;

                _bufferedWaveProvider.AddSamples(e.Buffer, bytesProcessed, length);

                if (_bufferedWaveProvider.BufferedBytes == _bufferedWaveProvider.BufferLength)
                {
                    int count = _sampleProvider.Read(_sampleBuffer, 0, SampleBufferSize);
                    DataAvailable?.Invoke(this, new WaveSample(_sampleBuffer, count));

                    _bufferedWaveProvider.ClearBuffer();
                }

                bytesToProcess -= length;
            }
        }

        public WaveFormat WaveFormat => _device.WaveFormat;

        public event EventHandler<WaveSample> DataAvailable;

        public void Dispose()
        {
            _device.Dispose();
        }

        public void Start()
        {
            _device.StartRecording();
        }

        public void Stop()
        {
            _device.StopRecording();
        }
    }
}
