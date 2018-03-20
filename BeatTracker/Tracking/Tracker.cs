using System;
using BeatTracker.Readers;

namespace BeatTracker.Tracking
{
    public partial class Tracker: IDisposable
    {
        private readonly IWaveStreamReader _source;

        public Tracker(IWaveStreamReader source)
        {
            _source = source;
        }

        public event EventHandler<BeatInfo> BeatInfoChanged; 

        public void Start()
        {
            this._source.DataAvailable += DataAvailable;
        }

        private void DataAvailable(object sender, SampleArgs e)
        {
            OnBeatInfoChanged(new BeatInfo(42));
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
