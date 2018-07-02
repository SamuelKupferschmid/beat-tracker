using System;
using System.Security.Principal;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using MathNet.Numerics;

namespace BeatTracker.Writers
{
    public class ConsoleWriter : SynchronizingWriter
    {
        public ConsoleWriter(ITracker tracker)
            : base(tracker)
        {
        }

        protected override void OnPulse(BeatInfo info)
        {
            Console.Beep(800,100);
        }
    }
}
