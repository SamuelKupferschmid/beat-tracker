using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;

namespace BeatTracker.Tracking
{
    /// <summary>
    /// Extracts periodic patterns
    /// </summary>
    public class PulseAnalyzer
    {
        private readonly FFTransformer _transformer;
        private readonly float[] _buffer = new float[1];

        public PulseAnalyzer()
        {
            _transformer = new FFTransformer(100);
            _transformer.FrameAvailable += _transformer_FrameAvailable;
        }

        public event EventHandler<IEnumerable<(float bpm, float confidence)>> PulseExtracted;

        private void _transformer_FrameAvailable(object sender, float[] e)
        {
            //TODO implement low pulse extraction
            PulseExtracted?.Invoke(this, new[]
            {
                (60f, 0.8f)
            });
        }

        public void AddFrame(float f)
        {
            _buffer[0] = f;
            _transformer.AddSample(new WaveSample(_buffer,1));
        }
    }
}
