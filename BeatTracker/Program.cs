using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using BeatTracker.Tracking;
using NAudio.Wave;

namespace BeatTracker
{
    class Program
    {
        static void Main(string[] args)
        {
            MonoWaveFileReader reader = new MonoWaveFileReader("data/ag1.wav");
            var tracker = new Tracker(reader);
            tracker.BeatInfoChanged += Tracker_BeatInfoChanged;
            tracker.Start();
        }

        private static void Tracker_BeatInfoChanged(object sender, BeatInfo e)
        {
            Console.WriteLine(e.Bpm);
        }
    }
}
