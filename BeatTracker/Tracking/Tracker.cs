using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BeatTracker.Readers;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Random;
using NAudio.Utils;
using NAudio.Wave;

namespace BeatTracker.Tracking
{
    public partial class Tracker : IDisposable
    {
        private readonly IWaveStreamReader _source;

        public Tracker(IWaveStreamReader source)
        {
            _source = source;

            FrequencyAnalyzer = new FrequencyAnalyzer(source);
            PulseAnalyzer = new PulseAnalyzer();

            FrequencyAnalyzer.FrameAvailable += (sender, frameValue) => PulseAnalyzer.AddFrame(frameValue);
            PulseAnalyzer.PulseExtracted += PulseAnalyzerOnPulseExtracted;
        }

        public event EventHandler<BeatInfo> BeatInfoChanged;

        public FrequencyAnalyzer FrequencyAnalyzer { get; }

        public BeatInfo BeatInfo { get; private set; }

        public PulseAnalyzer PulseAnalyzer { get; }

        public void Start()
        {
            _source.Start();
        }

        private void PulseAnalyzerOnPulseExtracted(object o, IEnumerable<(float bpm, float confidence)> candidates)
        {
            var bpm = candidates.OrderByDescending(c => c.confidence).First().bpm;

            OnBeatInfoChanged(new BeatInfo(bpm));
        }

        protected virtual void OnBeatInfoChanged(BeatInfo e)
        {
            if (BeatInfo == null || e.Bpm != BeatInfo.Bpm)
            {
                BeatInfo = e;
                BeatInfoChanged?.Invoke(this, e);
            }
        }

        public void Dispose()
        {
        }
    }
}