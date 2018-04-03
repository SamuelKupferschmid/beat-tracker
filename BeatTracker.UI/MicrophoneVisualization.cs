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
    class MicrophoneVisualization : ApplicationContext
    {
        public MicrophoneVisualization()
        {
            var logger = SpectrumLogger.Create("Device 0");
            var reader = new WaveInputDeviceReader(0);

            reader.DataAvailable += (o, e) => logger.AddSampe(e.Data);

            foreach (var inst in SpectrumLogger.Instances)
            {
                var form = new SpectrumVisualization();
                form.Text = inst.Name;
                inst.OnFrame += form.AddFrame;
                form.Show();
                form.FormClosing += (sender, args) =>
                {
                    inst.OnFrame -= form.AddFrame;
                    StopApplication();
                };
            }

            reader.Start();
        }

        private void StopApplication()
        {
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}