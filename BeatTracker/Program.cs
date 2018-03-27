using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatTracker.Readers;
using BeatTracker.Tracking;
using BeatTracker.Writers;
using NAudio.Wave;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using Timer = System.Timers.Timer;

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
