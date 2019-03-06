using BeatTracker.Readers;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using System;

namespace BeatTracker.DFTPrototype
{
    public class DftTracker : ITracker, IDisposable
    {
        private readonly IWaveStreamReader _source;
        private readonly IDateTime _dateTime;

        private readonly NaiveDftPipeline _pipeline;

        public DftTracker(IWaveStreamReader source)
            : this(source, new HighResolutionDateTime())
        {
        }

        public DftTracker(IWaveStreamReader source, IDateTime dateTime)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));

            _pipeline = new NaiveDftPipeline(44100, dateTime);
            _pipeline.BeatIdentified += Pipeline_BeatIdentified;
            _source.DataAvailable += Source_DataAvailable;
        }

        public event EventHandler<BeatInfo> BeatInfoChanged;

        public FrequencyAnalyzer FrequencyAnalyzer { get; }

        public BeatInfo BeatInfo { get; private set; }

        public PulseAnalyzer PulseAnalyzer { get; }

        public void Start()
        {
            _source.Start();
        }

        private void Source_DataAvailable(object sender, WaveSamples e)
        {
            _pipeline.Write(e.Data, e.Length);
        }

        private void Pipeline_BeatIdentified(BeatInfo beatInfo)
        {
            OnBeatInfoChanged(beatInfo);
        }

        protected virtual void OnBeatInfoChanged(BeatInfo e)
        {
            if (BeatInfo == null
                || Math.Abs(BeatInfo.Bpm - e.Bpm) > 0.5f)
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
