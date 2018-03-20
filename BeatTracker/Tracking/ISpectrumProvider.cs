using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Tracking
{
    public interface ISpectrumProvider
    {
        IList<float[]> Samples { get; }

        string Name { get; }

        double SampleResolution { get; }
        string SampleUnit { get; }

        event EventHandler DataChanged;
    }
}
