using System;
using System.Diagnostics;
using System.Threading;

namespace BeatTracker.Timers
{
    /// <summary>
    /// A high resolution timer based on System.Diagnostics.Stopwatch
    /// Source 1: https://stackoverflow.com/questions/7137121/high-resolution-timer/45097518#45097518
    /// Source 2: https://stackoverflow.com/a/41697139/548894
    /// </summary>
    public class StopwatchTimer : ITimer
    {
        /// <summary>
        /// Tick time length in [ms]
        /// </summary>
        public static readonly double TickLength = 1000f / Stopwatch.Frequency;

        /// <summary>
        /// Tick frequency
        /// </summary>
        public static readonly double Frequency = Stopwatch.Frequency;

        /// <summary>
        /// True if the system/operating system supports HighResolution timer
        /// </summary>
        public static bool IsHighResolution = Stopwatch.IsHighResolution;

        public event EventHandler Elapsed;

        /// <summary>
        /// The interval of timer
        /// </summary>
        private double _intervalInMilliseconds;

        private bool _isRunning;

        /// <summary>
        ///  Execution thread
        /// </summary>
        private Thread _thread;

        /// <summary>
        /// The interval of a timer in [ms]
        /// </summary>
        public TimeSpan Interval => TimeSpan.FromMilliseconds(_intervalInMilliseconds);

        public bool IsRunning => _isRunning;

        public void StartNew(TimeSpan interval)
        {
            if (_isRunning)
                throw new InvalidOperationException("Timer is already running");

            var milliseconds = interval.TotalMilliseconds;

            if (milliseconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(interval));

            _intervalInMilliseconds = milliseconds;

            _isRunning = true;
            _thread = new Thread(ExecuteTimer)
            {
                IsBackground = true,
            };
                        
            _thread.Start();
        }

        public void Stop()
        {
            _isRunning = false;

            if (Thread.CurrentThread != _thread)
            {
                _thread.Join();
            }
        }

        private void ExecuteTimer()
        {
            var nextTrigger = 0d;

            var sw = Stopwatch.StartNew();

            while (_isRunning)
            {
                nextTrigger += _intervalInMilliseconds;
                double elapsed;

                while (true)
                {
                    elapsed = ElapsedHiRes(sw);
                    double diff = nextTrigger - elapsed;

                    if (diff <= 0d)
                        break;

                    if (diff < 1d)
                        Thread.SpinWait(10);
                    else if (diff < 5d)
                        Thread.SpinWait(100);
                    else if (diff < 15d)
                        Thread.Sleep(1);
                    else
                        Thread.Sleep(10);

                    if (!_isRunning)
                        return;
                }
                
                Elapsed?.Invoke(this, EventArgs.Empty);

                if (!_isRunning)
                    return;

                // restarting the timer in every hour to prevent precision problems
                if (sw.Elapsed.TotalHours >= 1d)
                {
                    sw.Restart();
                    nextTrigger = 0d;
                }
            }

            sw.Stop();
        }

        private static double ElapsedHiRes(Stopwatch stopwatch)
        {
            return stopwatch.ElapsedTicks * TickLength;
        }
    }
}
