using BeatTracker.Timers;
using BeatTracker.Tracking;
using BeatTracker.Writers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Test.Writers
{
    public class MockSynchronizingWriter : SynchronizingWriter
    {
        private int _onPulseActionCallCount;
        private readonly Action _onPulseAction;

        public MockSynchronizingWriter(ITracker tracker, ITimer timer, IDateTime dateTime, Action onPulseAction) 
            : this(tracker, timer, dateTime)
        {
            _onPulseAction = onPulseAction ?? throw new ArgumentNullException(nameof(onPulseAction));
        }

        public MockSynchronizingWriter(ITracker tracker, ITimer timer, IDateTime dateTime)
            : base(tracker, timer, dateTime)
        {
        }

        public int OnPulseActionCallCount => _onPulseActionCallCount;

        protected override void OnPulse()
        {            
            _onPulseAction?.Invoke();
            ++_onPulseActionCallCount;
        }
    }
}
