using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Tracking.NoveltyCurve
{
    public class NoveltyParameters
    {
        public int StepSize { get; set; }

        public int WindowLength { get; set; }

        public int FeatureRate => DestinationSampleRate / StepSize;

        public int DestinationSampleRate { get; set; }
    }
}
