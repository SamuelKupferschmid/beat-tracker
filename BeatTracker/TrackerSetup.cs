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
        public static Tracker Create()
        {
            MonoWaveFileReader reader = new MonoWaveFileReader("data/ag1.wav");

            var dateTime = new HighResolutionDateTime();

            var tracker = new Tracker(reader, dateTime);

            return tracker;
        }

        //static void Main(string[] args)
        //{
        //    ProcessPriority.SetCurrentProcessPriorityToHigh();

        //    MonoWaveFileReader reader = new MonoWaveFileReader("data/ag1.wav");

        //    var timer = new MultimediaTimer();
        //    var dateTime = new HighResolutionDateTime();

        //    var tracker = new Tracker(reader, dateTime);

        //    var output = new MidiMetronomeWriter();

        //    var pulser = new SynchronizingPulser(tracker, timer, dateTime, output);


        //    // var output = new ConsoleWriter(tracker);

        //    tracker.Start();

        //    Console.ReadLine();
        //}
    }
}