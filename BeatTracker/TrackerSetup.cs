using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Helpers;
using BeatTracker.Readers;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using BeatTracker.Utils;
using BeatTracker.Writers;

namespace BeatTracker
{
    public class TrackerSetup
    {
        public static Tracker CreateWith<TWriter>(IWaveStreamReader reader) where TWriter : SynchronizingWriter
        {
            // ToDo: Move following line to a better place (for e.g. something like App.Init())
            ProcessPriority.SetCurrentProcessPriorityToHigh();

            var tracker = new Tracker(reader);
            var writer = Activator.CreateInstance(typeof(TWriter), tracker);

            return tracker;
        }

        public static ITimer GetNewTimerInstance() => new MultimediaTimer();

        public static IDateTime GetNewDateTimeInstance() => new HighResolutionDateTime();
    }
}