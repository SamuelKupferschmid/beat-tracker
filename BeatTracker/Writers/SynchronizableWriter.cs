using System;
using System.Timers;
using BeatTracker.Tracking;

namespace BeatTracker.Writers
{
    public abstract class SynchronizableWriter
    {
        public Tracker Tracker { get; }

        protected SynchronizableWriter(Tracker tracker)
        {
            Tracker = tracker;
        }

        public virtual void Start()
        {
            Tracker.BeatInfoChanged += TrackerOnBeatInfoChanged;
        }

        private void TrackerOnBeatInfoChanged(object sender, BeatInfo beatInfo)
        {
            //throw new NotImplementedException();
        }

        protected abstract void OnPulse();

        public TimeSpan Offset { get; set; }

        protected TimeSpan InternalOffset { get; set; }
    }
}
