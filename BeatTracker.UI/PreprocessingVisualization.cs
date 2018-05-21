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
            //** Pulse Block Start

            //var pulseAnalyzer = new PulseAnalyzer();

            //Task.Run(() =>
            //{
            //    var dir = @"C:\Program Files\MATLAB\R2016b\toolbox\tempogram\";
            //    var file = "Debussy_SonataViolinPianoGMinor-02_111_20080519-SMD-ss135-189.wav.noveltycurve_padded";
            //    var noveltyCurveExport = System.IO.File.ReadAllText(System.IO.Path.Combine(dir, file));
            //    var noveltyCurveValues = noveltyCurveExport.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => float.Parse(x)).ToList();
            //    noveltyCurveValues.ForEach(x => pulseAnalyzer.AddFrame(x));
            //});

            //** Pulse Block End

            var reader = new MonoWaveFileReader("data/ag1.wav");

            tracker = TrackerSetup.CreateWith<ConsoleWriter>(reader);

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