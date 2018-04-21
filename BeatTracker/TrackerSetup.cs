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
        public static Tracker Create(IWaveStreamReader reader, IPulseReceiver writer)
        {
            // ToDo: Move following line to a better place (for e.g. something like App.Init())
            ProcessPriority.SetCurrentProcessPriorityToHigh();
                      
            // MonoWaveFileReader reader = new MonoWaveFileReader("data/220_440_3520_sine.wav");
            MonoWaveFileReader reader = new MonoWaveFileReader("data/ag1.wav");
          
            var tracker = new Tracker(reader, new HighResolutionDateTime());

            // Note: Not happy with "hidden" pulser
            var pulser = new SynchronizingPulser(tracker, new MultimediaTimer(), new HighResolutionDateTime(), writer);
            pulser.Start();

            return tracker;
        }
    }
}