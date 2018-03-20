using System.Threading;
using BeatTracker.Tracking;
using Sanford.Multimedia.Midi;

namespace BeatTracker.Writers
{
    class MidiMetronomeWriter : SynchronizableWriter
    {
        public MidiMetronomeWriter(Tracker tracker) : base(tracker)
        {
        }

        protected override void OnPulse()
        {
            throw new System.NotImplementedException();
        }
    }
}