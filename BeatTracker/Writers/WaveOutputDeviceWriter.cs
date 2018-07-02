using BeatTracker.Tracking;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Writers
{
    public class WaveOutputDeviceWriter : SynchronizingWriter, IDisposable
    {
        private readonly WaveOutEvent _device;
        private readonly BufferedWaveProvider _bufferedWaveProvider;

        private readonly byte[] _onPulseWaveBuffer;

        /// <summary>
        /// Creates a WaveOutputDeviceWriter with the 'Click.wav' file and the standard output format (PCM 44.1Khz stereo 16 bit).
        /// </summary>
        public WaveOutputDeviceWriter(ITracker tracker, int deviceId)
            : this(tracker, deviceId, System.IO.File.ReadAllBytes(@"data\Click.wav"), new WaveFormat())
        {
        }

        public WaveOutputDeviceWriter(ITracker tracker, int deviceId, byte[] onPulseWaveBuffer, WaveFormat waveFormat)
            : base(tracker)
        {
            if (deviceId < 0 || deviceId > WaveOut.DeviceCount - 1)
                throw new ArgumentException(nameof(deviceId));

            if (onPulseWaveBuffer == null || onPulseWaveBuffer.Length == 0)
                throw new ArgumentException(nameof(onPulseWaveBuffer));

            _onPulseWaveBuffer = onPulseWaveBuffer;
            
            _device = new WaveOutEvent();
            _device.DeviceNumber = deviceId;

            var onPulseWaveDuration = TimeSpan.FromSeconds((double)_onPulseWaveBuffer.Length / waveFormat.AverageBytesPerSecond);

            _device.DesiredLatency = (int)onPulseWaveDuration.TotalMilliseconds;

            _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);

            _device.Init(_bufferedWaveProvider);
        }

        public WaveFormat OutputWaveFormat => _device.OutputWaveFormat;

        protected override void OnPulse(BeatInfo info)
        {
            // Findings:
            // Does not work well when onPulseWaveDuration is greater than TimeSpan.FromSeconds(1 / bpm).


            _bufferedWaveProvider.AddSamples(_onPulseWaveBuffer, 0, _onPulseWaveBuffer.Length);
        }

        public void Dispose()
        {
            _device.Dispose();
        }

        public override void Start()
        {
            base.Start();
            _device.Play();
        }

        public override void Stop()
        {
            base.Stop();
            _device.Stop();
        }
    }
}
