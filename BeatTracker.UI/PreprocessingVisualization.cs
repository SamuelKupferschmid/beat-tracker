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

            var pulseAnalyzer = new PulseAnalyzer();

            Task.Run(() =>
            {
                var dir = @"C:\Program Files\MATLAB\R2016b\toolbox\tempogram\";
                //var file = "Debussy_SonataViolinPianoGMinor-02_111_20080519-SMD-ss135-189.wav.noveltycurve";
                var file = "110-130bpm_click.wav.noveltycurve";
                var noveltyCurveExport = System.IO.File.ReadAllText(System.IO.Path.Combine(dir, file));
                var noveltyCurveValues = noveltyCurveExport.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => float.Parse(x)).ToList();
                noveltyCurveValues.ForEach(x =>
                {
                    // Thread.Sleep((int)TimeSpan.FromMilliseconds((double)32000 / noveltyCurveValues.Count).TotalMilliseconds);

                    Thread.Sleep(5);
                    pulseAnalyzer.AddFrame(x);

                });
            });

            //** Pulse Block End

            var reader = new MonoWaveFileReader("data/110-130bpm_click_new.wav", isSourceStereo: false);
            tracker = TrackerSetup.CreateWith<ConsoleWriter>(reader);
            tracker.Start();

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

            //thread = new Thread(tracker.Start);
            //thread.Start();
        }

        private void StopApplication()
        {
            //tracker.Stop();
            //thread.Abort();
            //thread.Join();
            //Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}