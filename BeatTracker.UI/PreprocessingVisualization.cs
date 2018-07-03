using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeatTracker.Readers;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using BeatTracker.Utils;
using BeatTracker.Writers;

namespace BeatTracker.UI
{
    class PreprocessingVisualization : ApplicationContext
    {
        private readonly Thread thread;
        private readonly Tracker tracker;

        public PreprocessingVisualization()
        {
            //var reader = new MonoWaveFileReader("data/110-130bpm_click.wav", isSourceStereo: false);
            //var reader = new MonoWaveFileReader("data/Albums-Ballroom_Classics4-01.wav", isSourceStereo: false);
            var reader = new WaveInputDeviceReader(0);
            tracker = TrackerSetup.CreateWith<MidiMetronomeWriter>(reader);

            foreach (var inst in SpectrumLogger.Instances)
            {
                var form = new SpectrumVisualization { Text = inst.Name };
                inst.OnFrame += form.AddFrame;
                form.Show();
                form.FormClosing += (sender, args) =>
                {
                    inst.OnFrame -= form.AddFrame;
                    StopApplication();
                };
            }

            thread = new Thread(tracker.Start);
            thread.Start();
        }

        private void StopApplication()
        {
            tracker.Stop();
            thread.Abort();
            thread.Join();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}