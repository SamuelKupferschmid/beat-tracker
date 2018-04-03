using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using BeatTracker.Helpers;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatTracker.Writers
{
    public sealed class SynchronizingPulser
    {
        // Periodic time in [ms] in which writer checks for OnPulse() calls.
        public const int Resolution = 5;

        private readonly ITracker _tracker;
        private readonly ITimer _timer;
        private readonly IDateTime _dateTime;
        private readonly IPulseReceiver _pulseReceiver;

        private BeatInfo _currentBeatInfo;
        private ConcurrentSlidingEnumerable<double> _recentOnPulseCallTimes;

        private readonly object _syncRoot = new object();

        public SynchronizingPulser(ITracker tracker, ITimer timer, IDateTime dateTime, IPulseReceiver pulseReceiver)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _timer = timer ?? throw new ArgumentNullException(nameof(tracker));
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
            _pulseReceiver = pulseReceiver ?? throw new ArgumentNullException(nameof(pulseReceiver));

            _recentOnPulseCallTimes = new ConcurrentSlidingEnumerable<double>(20);

            _tracker.BeatInfoChanged += Tracker_BeatInfoChanged;
            _timer.Elapsed += Timer_Elapsed;
        }

        public TimeSpan Offset { get; set; } = TimeSpan.Zero;

        public TimeSpan InternalOffset => TimeSpan.FromMilliseconds(_recentOnPulseCallTimes.Average());

        public DateTime CallOnPulseAt
        {
            get
            {
                var copy = _currentBeatInfo;
                if (copy != null)
                {
                    var occursAt = copy.OccursAt;
                    var bpmTimeSpan = TimeSpan.FromMinutes(1 / copy.Bpm);
                    var now = _dateTime.Now;

                    if (occursAt + Offset + InternalOffset < now)
                    {                        
                        occursAt = now + TimeSpan.FromTicks((now - occursAt).Ticks % bpmTimeSpan.Ticks);

                        lock (_syncRoot)
                        {
                            if (copy == _currentBeatInfo)
                                _currentBeatInfo = new BeatInfo(copy.Bpm, occursAt);
                        }
                    }

                    return (occursAt + Offset + InternalOffset);
                }

                // This causes OnPulse() to be never called.
                return DateTime.MaxValue;
            }
        }

        public void Start()
        {
            _timer.StartNew(TimeSpan.FromMilliseconds(Resolution));
        }

        private void Tracker_BeatInfoChanged(object sender, BeatInfo e)
        {
            lock (_syncRoot)
            {
                _currentBeatInfo = e;
            }
        }
        
        private void Timer_Elapsed(object sender, EventArgs e)
        {
            var offset = Math.Abs((_dateTime.Now - CallOnPulseAt).TotalMilliseconds);

//#if DEBUG
//            System.Diagnostics.Debug.Print($"Offset: {offset:F}ms.");
//#endif

            if (offset < Resolution)
            {
                var lastOnPulseCall = _dateTime.Now;

                _pulseReceiver.OnPulse();

                var onPulseCallTime = (_dateTime.Now - lastOnPulseCall).TotalMilliseconds;
                _recentOnPulseCallTimes.Push(onPulseCallTime);
            }
        }
    }
}
