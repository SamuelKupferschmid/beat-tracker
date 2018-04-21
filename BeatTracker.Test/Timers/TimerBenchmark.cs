using BeatTracker.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatTracker.Test.Timers
{
    public class TimerBenchmark
    {
        private readonly ITimer _timer;
        private readonly TimeSpan _interval;
        private readonly TimeSpan _runTime;

        // Tolerance in [ms]
        private readonly double _toleranceInMilliseconds;

        public TimerBenchmark(ITimer timer, TimeSpan interval, TimeSpan runTime, double toleranceInMilliseconds)
        {
            _timer = timer ?? throw new ArgumentNullException(nameof(timer));

            if (interval.Ticks <= 0)
                throw new ArgumentException(nameof(interval));
            _interval = interval;
            
            if (runTime.Ticks <= 0)
                throw new ArgumentException(nameof(runTime));
            _runTime = runTime;

            if (toleranceInMilliseconds <= 0)
                throw new ArgumentException(nameof(toleranceInMilliseconds));
            _toleranceInMilliseconds = toleranceInMilliseconds;
        }

        public TimeSpan Interval => _interval;

        public TimeSpan RunTime => _runTime;

        public double ToleranceInMilliseconds => _toleranceInMilliseconds;

        public float AverageCpuUsed { get; private set; }

        public float MaxCpuUsed { get; private set; }

        public float MinCpuUsed { get; private set; }

        public float AverageMemoryUsed { get; private set; }

        public float MaxMemoryUsed { get; private set; }

        public float MinMemoryUsed { get; private set; }

        public double AverageDeviation { get; private set; }

        public double MaxDeviation { get; private set; }

        public double MinDeviation { get; private set; }

        public double Accuracy { get; private set; }

        public void Run()
        {
            if (!Stopwatch.IsHighResolution)
                throw new NotSupportedException("Cannot measure accurately when Stopwatch.IsHighResolution is false.");

            var sync = new ManualResetEvent(false);

            var processName = Process.GetCurrentProcess().ProcessName;

            var cpuCounter = new PerformanceCounter("Process", "% Processor Time", processName);
            var memCounter = new PerformanceCounter("Process", "Working Set", processName);

            var deviationMeasures = new List<double>();
            var cpuMeasures = new List<float>();
            var memMeasures = new List<float>();

            var random = new Random();

            int counter = 0;

            var stopTime = DateTime.Now + _runTime;

            var sw = Stopwatch.StartNew();

            var last = TimeSpan.Zero;

            var runningSeconds = 0;

            _timer.Elapsed += (o, e) =>
            {
                var elapsed = sw.Elapsed;
                var deviation = Math.Abs((elapsed - last).TotalMilliseconds - Interval.TotalMilliseconds);
                last = elapsed;

                Task.Run(() =>
                {
                    if (DateTime.Now < stopTime)
                    {
                        lock (deviationMeasures)
                        {
                            deviationMeasures.Add(deviation);
                        }

                        if (deviation > _toleranceInMilliseconds)
                            System.Diagnostics.Debug.Print($"Deviation (ms) exceeds tolerance: {deviation:F3}ms.");

                        // According to 'https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter.nextvalue.aspx'
                        // Query every 1s for accurate results.

                        if (sw.Elapsed.Seconds > runningSeconds)
                        {
                            lock (cpuMeasures)
                            {
                                cpuMeasures.Add(cpuCounter.NextValue() / Environment.ProcessorCount); // in percent, for e.g. 10.00%
                                memMeasures.Add(memCounter.NextValue()); // in Bytes

                                ++runningSeconds;
                            }
                        }
                    }
                    else 
                    {
                        if (_timer.IsRunning)
                            _timer.Stop();

                        sync.Set();
                    }
                });
            };

            _timer.StartNew(_interval);

            sync.WaitOne();

            // Calculate some statistics

            if (cpuMeasures.Any())
            {
                AverageCpuUsed = cpuMeasures.Average();
                MaxCpuUsed = cpuMeasures.Max();
                MinCpuUsed = cpuMeasures.Min();
            }

            if (memMeasures.Any())
            {
                AverageMemoryUsed = memMeasures.Average();
                MaxMemoryUsed = memMeasures.Max();
                MinMemoryUsed = memMeasures.Min();
            }

            if (deviationMeasures.Any())
            {
                AverageDeviation = deviationMeasures.Average();
                MaxDeviation = deviationMeasures.Max();
                MinDeviation = deviationMeasures.Min();

                Accuracy = deviationMeasures.Count(d => d < _toleranceInMilliseconds) / (double)deviationMeasures.Count;
            }
        }
    }
}
