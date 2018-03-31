using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Timers
{
    public interface ITimer
    {
        bool IsRunning { get; }

        void StartNew(TimeSpan interval);

        void Stop();

        event EventHandler Elapsed;
    }
}
