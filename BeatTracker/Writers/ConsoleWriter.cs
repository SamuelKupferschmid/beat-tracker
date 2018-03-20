using System;
using BeatTracker.Tracking;

namespace BeatTracker.Writers
{
    public class ConsoleWriter: SynchronizableWriter
    {
        public ConsoleWriter(Tracker tracker) : base(tracker)
        {
        }

        protected override void OnPulse()
        {
            Console.WriteLine("foo..");
        }
    }
}
