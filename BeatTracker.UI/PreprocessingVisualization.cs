using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeatTracker.DFTPrototype;
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
        private readonly ITracker _tracker;

        public PreprocessingVisualization()
        {
            //var reader = new MonoWaveFileReader("data/110-130bpm_click.wav", isSourceStereo: false);
            //var reader = new MonoWaveFileReader("data/Albums-Ballroom_Classics4-01.wav", isSourceStereo: false);

            //var reader = new MonoWaveFileReader("data/10894322.clip.mp3_repeated_60sec.wav", isSourceStereo: false);
            //var reader = new MonoWaveFileReader("data/SeanPaul-Breathe-Clean.wav", isSourceStereo: true);

            //var reader = new WaveInputDeviceReader(0);
            //tracker = TrackerSetup.CreateWith<MidiMetronomeWriter>(reader);

            var reader = new WaveInputDeviceReader(0);
            //_tracker = new Tracker(reader);
            _tracker = new DftTracker(reader);

            foreach (var inst in SpectrumLogger.Instances)
            {
                //continue;

                var form = new SpectrumVisualization { Text = inst.Name };
                inst.OnFrame += form.AddFrame;
                form.Show();
                form.FormClosing += (sender, args) =>
                {
                    inst.OnFrame -= form.AddFrame;
                    StopApplication();
                };
            }

            thread = new Thread(_tracker.Start);
            thread.Start();
            //thread.Join();
        }

        private void StopApplication()
        {
            _tracker.Stop();
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