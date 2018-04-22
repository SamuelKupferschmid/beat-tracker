using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using BeatTracker.Utils;
using MathNet.Numerics.IntegralTransforms;

namespace BeatTracker.Tracking
{
    /// <summary>
    /// Extracts periodic patterns
    /// </summary>
    public class PulseAnalyzer
    {
        private readonly FFTransformer _transformer;
        private readonly float[] _buffer = new float[1];

        private readonly SpectrumLogger _outputLogger = SpectrumLogger.Create("PulseAnalyzer");

        public PulseAnalyzer()
        {
            var frequencyAnalyzerRate = 2;

            var fftWindowSize = 8 * frequencyAnalyzerRate;
            var fftStepSize = 1;

            // How to map to BPM 30? (0.5 Hz)
            
            var minBpm = 30;
            var maxBpm = 600;

            _transformer = new FFTransformer(fftWindowSize, fftStepSize, 1, 10);
            _transformer.FrameAvailable += _transformer_FrameAvailable;
        }

        public event EventHandler<IEnumerable<(float bpm, float confidence)>> PulseExtracted;

        private void _transformer_FrameAvailable(object sender, float[] e)
        {
            var hz = GetMaxHz(e);
            var confidence = e[hz];
            var bpm = hz * 60;

            _outputLogger.AddSampe(e);

            PulseExtracted?.Invoke(this, new[]
            {
                ((float)bpm, (float)confidence)
            });
        }

        public void AddFrame(float f)
        {
            _buffer[0] = f;
            _transformer.AddSamples(new WaveSamples(_buffer,_buffer.Length));
        }

        private int GetMaxHz(float[] e)
        {
            int hz = 0;
            float confidence = float.MinValue;

            for (int i = 0; i < e.Length; i++)
            {
                if (e[i] > confidence)
                {
                    hz = i;
                    confidence = e[i];
                }
            }

            return hz;
        }
    }
}
