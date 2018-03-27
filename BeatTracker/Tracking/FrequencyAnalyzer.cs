using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using MathNet.Numerics.Statistics;

namespace BeatTracker.Tracking
{
    /// <summary>
    /// Extracts a curve from given stream which highlights beats in signals
    /// </summary>
    public class FrequencyAnalyzer
    {
        private readonly IWaveStreamReader _streamReader;
        private readonly FFTransformer _transformer;

        public FrequencyAnalyzer(IWaveStreamReader streamReader)
        {
            _streamReader = streamReader;
            _transformer = new FFTransformer(100);
            _transformer.FrameAvailable += _transformer_FrameAvailable;
            _streamReader.DataAvailable += _streamReader_DataAvailable;
        }

        public event EventHandler<float> FrameAvailable; 

        private void _streamReader_DataAvailable(object sender, WaveSample e)
        {
            _transformer.AddSample(e);
        }

        private void _transformer_FrameAvailable(object sender, float[] e)
        {
            //TODO implement novalty curve extraction
            FrameAvailable?.Invoke(this, e.Select(Math.Abs).Sum());
        }
    }
}
