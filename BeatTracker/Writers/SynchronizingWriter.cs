using BeatTracker.Helpers;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using System;
using System.Diagnostics;
using System.Linq;

namespace BeatTracker.Writers
{
    public abstract class SynchronizingWriter
    {
        public const int IntervalInMilliseconds = 1;
        public const int ResolutionInMilliseconds = 5;

        private readonly ITracker _tracker;
        private readonly ITimer _timer;
        private readonly IDateTime _dateTime;

        private BeatInfo _currentBeatInfo;
        private ConcurrentSlidingEnumerable<double> _recentOnPulseCallDurations;

        private readonly object _syncRoot = new object();

        public SynchronizingWriter(ITracker tracker)
            : this(tracker, TrackerSetup.GetNewTimerInstance(), TrackerSetup.GetNewDateTimeInstance())
        {
        }

        public SynchronizingWriter(ITracker tracker, ITimer timer, IDateTime dateTime)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _timer = timer ?? throw new ArgumentNullException(nameof(tracker));
            _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));

            _recentOnPulseCallDurations = new ConcurrentSlidingEnumerable<double>(20);

            _tracker.BeatInfoChanged += Tracker_BeatInfoChanged;
            _timer.Elapsed += Timer_Elapsed;
        }

        public TimeSpan Offset { get; set; } = TimeSpan.Zero;

        public TimeSpan AverageOnPulseDuration => TimeSpan.FromMilliseconds(_recentOnPulseCallDurations.Average());

        public TimeSpan InternalOffset
        {
            get
            {
                // ToDo: Calculate InternalOffset by Alignment to the Bpm
                return TimeSpan.Zero;
            }
        }

        public DateTime GetNextOnPulseCallTime()
        {
            var copy = _currentBeatInfo;
            if (copy != null && copy.Bpm > 0)
            {
                var occursAt = copy.OccursAt;
                var now = _dateTime.Now;
                var internalOffset = InternalOffset;

                var resolution = TimeSpan.FromMilliseconds(ResolutionInMilliseconds);

                if ((now - (occursAt + Offset + internalOffset)) > resolution)
                {
                    var bpm = TimeSpan.FromMinutes(1 / copy.Bpm);

                    while (occursAt + Offset + internalOffset + resolution < now)
                        occursAt += bpm;

                    lock (_syncRoot)
                    {
                        if (copy == _currentBeatInfo)
                            _currentBeatInfo = new BeatInfo(copy.Bpm, occursAt, copy.Confidence);
                    }
                }

                return (occursAt + Offset + internalOffset);
            }

            // This causes OnPulse() to be never called.
            return DateTime.MaxValue;            
        }
        
        public virtual void Start()
        {
            _timer.StartNew(TimeSpan.FromMilliseconds(IntervalInMilliseconds));
        }

        public virtual void Stop()
        {
            _timer.Stop();
        }

        protected abstract void OnPulse(BeatInfo info);

        private void Tracker_BeatInfoChanged(object sender, BeatInfo e)
        {
            lock (_syncRoot)
            {
                _currentBeatInfo = e;
            }

            if (!_timer.IsRunning)
                Start();
        }

#if DEBUG
        private DateTime? _lastOnPulseCallTime;
        private int _onPulseCallCount;
        private int _onPulseSkipCount;
        private DateTime _elapsed;
#endif

        private void Timer_Elapsed(object sender, EventArgs e)
        {           
            var sw = Stopwatch.StartNew();

            var callOnPulseAt = GetNextOnPulseCallTime();
            var offset = Math.Abs((_dateTime.Now - callOnPulseAt).TotalMilliseconds);

#if DEBUG
            //System.Diagnostics.Debug.Print($"Offset: {offset:F}ms | CallOnPulseAt: {callOnPulseAt:HH:mm:ss.ffff} ");            
            //System.Diagnostics.Debug.Print($"Elapsed: {(_dateTime.Now - _elapsed).TotalMilliseconds:F}ms.");
            //_elapsed = _dateTime.Now;
#endif

            if (offset < ResolutionInMilliseconds)
            {
                lock (_syncRoot)
                {
                    if (_currentBeatInfo.Bpm > 0)
                        _currentBeatInfo = new BeatInfo(_currentBeatInfo.Bpm, _currentBeatInfo.OccursAt + TimeSpan.FromMinutes(1 / _currentBeatInfo.Bpm), _currentBeatInfo.Confidence);
                }

#if DEBUG
                if (_lastOnPulseCallTime.HasValue && _currentBeatInfo.Bpm > 0)
                {
                    ++_onPulseCallCount;

                    var now = _dateTime.Now;
                    var diff = now - _lastOnPulseCallTime.Value;
                    var bpm = TimeSpan.FromMinutes(1 / _currentBeatInfo.Bpm);
                    var off = Math.Abs(bpm.TotalMilliseconds - diff.TotalMilliseconds);
                    _lastOnPulseCallTime = now;

                    _onPulseSkipCount += (int)Math.Round(off / bpm.TotalMilliseconds);

                    System.Diagnostics.Debug.Print($"OnPulse: {_dateTime.Now:HH:mm:ss.ffff} | Diff: {diff.TotalMilliseconds:F}ms | Off: {off:F3}ms | Skip % ({_onPulseSkipCount}/{_onPulseSkipCount + _onPulseCallCount}): {(double)_onPulseSkipCount / (_onPulseSkipCount + _onPulseCallCount) * 100:F}.");
                }
                else
                    _lastOnPulseCallTime = _dateTime.Now;
#endif

                OnPulse(_currentBeatInfo);

                _recentOnPulseCallDurations.Push(sw.Elapsed.TotalMilliseconds);
            }
        }
    }
}
