using BeatTracker.Readers;
using BeatTracker.Tracking;
using BeatTracker.Writers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeatTracker.UI
{
    public partial class Test : Form
    {
        public Test()
        {
            InitializeComponent();
            this.Shown += (o, e) =>
            {
                var reader = new WaveInputDeviceReader(0);
                var tracker = new Tracker(reader);

                //var writer = new WaveOutputDeviceWriter(tracker, 0);

                var writer = new MidiMetronomeWriter(tracker, 0);

                tracker.Start();
            };
        }
    }
}
