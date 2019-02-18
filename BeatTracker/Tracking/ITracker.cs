using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Tracking
{
    public interface ITracker
    {
        event EventHandler<BeatInfo> BeatInfoChanged;

        void Start();

        void Stop();
    }
}
