using BeatTracker.Helpers;
using BeatTracker.Readers;
using BeatTracker.Tracking;
using BeatTracker.Writers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeatTracker.UI
{
    public partial class MainForm : Form
    {
        class VisualWriter : SynchronizingWriter
        {
            private readonly MainForm _mainForm;

            public VisualWriter(MainForm mainForm, ITracker tracker) : base(tracker)
            {
                _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            }

            protected override void OnPulse(BeatInfo info)
            {
                _mainForm.BeginInvoke(new Action(async () =>
                {
                    _mainForm.lblLaufzeit.Text = $"Laufzeit: {_mainForm._stopwatch.Elapsed}";
                    _mainForm.lblBPM.Text = $"Aktuelle BPM: {info.Bpm:F2}";
                    _mainForm.lblConfidence.Text = $"Konfidenz: {(info.Confidence * 100):F2}";
                    _mainForm.pnlCircle.BackColor = Color.Red;
                    await Task.Delay(150).ContinueWith(task =>
                    {
                        _mainForm.BeginInvoke(new Action(() =>
                        {
                            _mainForm.pnlCircle.BackColor = Color.Transparent;
                        }));
                    });
                }));
            }
        }

        WaveInputDeviceReader _reader;
        ITracker _tracker;
        VisualWriter _visualWriter;
        MidiMetronomeWriter _midiMetronomeWriter;

        Stopwatch _stopwatch;

        public MainForm()
        {
            InitializeComponent();

            ProcessPriority.SetCurrentProcessPriorityToHigh();

            btnStart.Enabled = true;
            btnStop.Enabled = false;

            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
        }                
        
        private void BtnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = true;

            //_reader = new MonoWaveFileReader("data/Media-103516.wav", isSourceStereo: false);


            //_reader = new MonoWaveFileReader("data/110-130bpm_click.wav", isSourceStereo: false);

            //_reader = new MonoWaveFileReader("data/Albums-Ballroom_Classics4-01.wav", isSourceStereo: false);

            _reader = new WaveInputDeviceReader(0);
            _tracker = new Tracker(_reader);
            _visualWriter = new VisualWriter(this, _tracker);
            //_midiMetronomeWriter = new MidiMetronomeWriter(_tracker, 0);
                        
            _stopwatch = Stopwatch.StartNew();
            _visualWriter.Start();
            _midiMetronomeWriter.Start();
            _reader.Start();
            //_reader.Start(simulatePlaybackspeed: true);
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;

            _stopwatch.Stop();

            if (_midiMetronomeWriter != null)
            {
                _midiMetronomeWriter.Stop();
                _midiMetronomeWriter.Dispose();
                _midiMetronomeWriter = null;
            }

            if (_visualWriter != null)
            {
                _visualWriter.Stop();
                _visualWriter = null;
            }

            if (_reader != null)
            {
                _reader.Stop();
                _reader = null;
            }

            _tracker = null;
        }
    }
}
