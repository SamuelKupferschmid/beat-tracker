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
        private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(1);
        private static readonly TimeSpan StopAfter = TimeSpan.FromMinutes(1);

        // Deviation Tolerance in Seconds: 10ms
        private static readonly float Tolerance = 0.01f;

        private readonly ITimer _timer;

        public TimerBenchmark(ITimer timer)
        {
            _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        }

        public float AverageCpuUsed { get; private set; }

        public float MaxCpuUsed { get; private set; }

        public float MinCpuUsed { get; private set; }

        public float AverageMemoryUsed { get; private set; }

        public float MaxMemoryUsed { get; private set; }

        public float MinMemoryUsed { get; private set; }

        public float AverageDeviation { get; private set; }

        public float MaxDeviation { get; private set; }

        public float MinDeviation { get; private set; }

        public float Accuracy { get; private set; }

        public void Run()
        {
            if (!Stopwatch.IsHighResolution)
                throw new NotSupportedException("Cannot measure accurately when Stopwatch.IsHighResolution is false.");

            var sync = new ManualResetEvent(false);

            var processName = Process.GetCurrentProcess().ProcessName;

            var cpuCounter = new PerformanceCounter("Process", "% Processor Time", processName);
            var memCounter = new PerformanceCounter("Process", "Working Set", processName);

            var deviationMeasures = new List<float>();
            var cpuMeasures = new List<float>();
            var memMeasures = new List<float>();

            var random = new Random();

            int counter = 0;
          
            var startTime = Stopwatch.GetTimestamp();
            var stopTime = startTime + (StopAfter.Ticks / TimeSpan.FromSeconds(1).Ticks) * Stopwatch.Frequency;

            var ticksPerElapsedEvent = (Interval.Ticks / (float)TimeSpan.FromSeconds(1).Ticks) * Stopwatch.Frequency;

            _timer.Elapsed += (o, e) =>
            {                
                var actual = Stopwatch.GetTimestamp();
                var expected = startTime + ((++counter) * ticksPerElapsedEvent);

                Task.Run(() =>
                {
                    if (actual < stopTime)
                    {
                        deviationMeasures.Add(Math.Abs(actual - expected) / Stopwatch.Frequency);

                        // According to 'https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter.nextvalue.aspx'
                        // Query every 1s for accurate results.

                        if (counter % (TimeSpan.FromSeconds(1).Ticks / Interval.Ticks) == 0)
                        {
                            cpuMeasures.Add(cpuCounter.NextValue() / Environment.ProcessorCount); // in percent, for e.g. 10.00%
                            memMeasures.Add(memCounter.NextValue()); // in Bytes
                        }
                    }
                    else 
                    {
                        _timer.Stop();
                        sync.Set();
                    }
                });
            };

            _timer.StartNew(Interval);

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

                Accuracy = deviationMeasures.Where(d => d < Tolerance).Count() / (float)deviationMeasures.Count;
            }
        }
    }
}
