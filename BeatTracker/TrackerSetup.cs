using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using BeatTracker.Tracking;
using BeatTracker.Utils;
using BeatTracker.Writers;

namespace BeatTracker
{
    public class TrackerSetup
    {
        public static Tracker Create()
        {
            MonoWaveFileReader reader = new MonoWaveFileReader("data/ag1.wav");

            var tracker = new Tracker(reader);
            // var output = new MidiMetronomeOutput(tracker);
            var output = new ConsoleWriter(tracker);

            output.Start();
            return tracker;
        }
    }
}