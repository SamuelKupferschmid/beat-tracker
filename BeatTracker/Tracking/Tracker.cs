﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BeatTracker.Readers;
using BeatTracker.Timers;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Random;
using NAudio.Utils;
using NAudio.Wave;

namespace BeatTracker.Tracking
{
    public partial class Tracker : ITracker, IDisposable
    {
        private readonly IWaveStreamReader _source;
        private readonly IDateTime _dateTime;

        public Tracker(IWaveStreamReader source)
            : this(source, new HighResolutionDateTime())
        {
        }

        public Tracker(IWaveStreamReader source, IDateTime dateTime)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));

            FrequencyAnalyzer = new FrequencyAnalyzer(_source);
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
            var best = candidates.OrderByDescending(c => c.confidence).First();

            // ToDo: Calculate 'OccursAt' (DateTime)?
            var occursAt = _dateTime.Now;

            OnBeatInfoChanged(new BeatInfo(best.bpm, occursAt, best.confidence));
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

        public void Stop()
        { 
            _source.Stop();
        }
    }
}