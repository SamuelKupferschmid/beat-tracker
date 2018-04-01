using System;
using BeatTracker.Readers;
using BeatTracker.Timers;

namespace BeatTracker.Tracking
{
    public partial class Tracker : ITracker, IDisposable
    {
        private readonly IWaveStreamReader _source;
        private readonly IDateTime _dateTime;

        public Tracker(IWaveStreamReader source, IDateTime dateTime)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
        }

        public event EventHandler<BeatInfo> BeatInfoChanged; 

        public void Start()
        {
            this._source.DataAvailable += DataAvailable;
        }

        private void DataAvailable(object sender, SampleArgs e)
        {
            OnBeatInfoChanged(new BeatInfo(42, _dateTime.Now));
        }

        public void Dispose()
        {
            this._source.DataAvailable -= DataAvailable;
        }

        protected virtual void OnBeatInfoChanged(BeatInfo e)
        {
            BeatInfoChanged?.Invoke(this, e);
        }
    }
}
