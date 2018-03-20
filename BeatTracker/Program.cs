using System;
using BeatTracker.Readers;
using BeatTracker.Tracking;
using BeatTracker.Writers;

namespace BeatTracker
{
    class Program
    {
        static void Main(string[] args)
        {

            MonoWaveFileReader reader = new MonoWaveFileReader("data/ag1.wav");

            var tracker = new Tracker(reader);
            
             var output = new MidiMetronomeWriter(tracker);
            // var output = new ConsoleWriter(tracker);

            output.Start();
            tracker.Start();

            Console.ReadLine();
        }
    }
}
