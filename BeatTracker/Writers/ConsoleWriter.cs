using System;
using BeatTracker.Timers;
using BeatTracker.Tracking;

namespace BeatTracker.Writers
{
    public class ConsoleWriter : IPulseReceiver
    {
        public void OnPulse()
        {
            Console.WriteLine("foo..");
        }
    }
}
