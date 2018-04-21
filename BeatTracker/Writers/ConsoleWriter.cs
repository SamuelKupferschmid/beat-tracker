using System;
using BeatTracker.Timers;
using BeatTracker.Tracking;

namespace BeatTracker.Writers
{
    public class ConsoleWriter : SynchronizingWriter
    {
        public ConsoleWriter(ITracker tracker)
            : base(tracker)
        {
        }

        protected override void OnPulse()
        {
            Console.WriteLine("foo..");
        }
    }
}
