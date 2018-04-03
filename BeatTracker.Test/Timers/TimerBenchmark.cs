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
        private static readonly TimeSpan StopAfter = TimeSpan.FromSeconds(60);

        // Tolerance in Milliseconds: 5ms
        public static readonly double Tolerance = 5;

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

            var stopTime = DateTime.Now + StopAfter;

            var sw = Stopwatch.StartNew();

            _timer.Elapsed += (o, e) =>
            {
                var deviation = sw.ElapsedMilliseconds;
                sw.Restart();

                Task.Run(() =>
                {
                    if (DateTime.Now < stopTime)
                    {
                        lock (deviationMeasures)
                        {
                            deviationMeasures.Add(deviation);
                        }

                        // According to 'https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter.nextvalue.aspx'
                        // Query every 1s for accurate results.

                        if (++counter % (TimeSpan.FromSeconds(1).Ticks / Interval.Ticks) == 0)
                        {
                            lock (cpuMeasures)
                            {
                                cpuMeasures.Add(cpuCounter.NextValue() / Environment.ProcessorCount); // in percent, for e.g. 10.00%
                                memMeasures.Add(memCounter.NextValue()); // in Bytes
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

                Accuracy = deviationMeasures.Count(d => d < Tolerance) / (float)deviationMeasures.Count;
            }
        }
    }
}
