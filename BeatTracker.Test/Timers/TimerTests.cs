using BeatTracker.Helpers;
using BeatTracker.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BeatTracker.Test.Timers
{
    [Collection("Synchronizing")]
    public class TimerTests
    {
        private const double RequiredPrecisionInMilliseconds = 0.005d;

        private readonly ITestOutputHelper _output;

        public TimerTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            
            ProcessPriority.SetCurrentProcessPriorityToHigh();
        }

        [Fact]
        public void IsMultimediaTimerBenchmarkPassingRequirements()
        {
            var timer = new MultimediaTimer();
            var interval = TimeSpan.FromMilliseconds(1);
            var runTime = TimeSpan.FromSeconds(60);
            var toleranceInMilliseconds = 4d;

            var benchmark = new TimerBenchmark(timer, interval, runTime, toleranceInMilliseconds);
            benchmark.Run();

            Print(nameof(MultimediaTimer), benchmark);

            Assert.True(benchmark.Accuracy > 0.99d);

            // Findings:
            // - No changes in setting difference interval parameters (1,2,...,5).
            // - No changes in setting longer runTime (>60s).
            // - More gains when toleranceInMilliseconds is raised.
        }

        [Fact]
        public void IsStopwatchTimerBenchmarkPassingRequirements()
        {
            var timer = new StopwatchTimer();
            var interval = TimeSpan.FromMilliseconds(1);
            var runTime = TimeSpan.FromSeconds(60);
            var toleranceInMilliseconds = 4d;

            var benchmark = new TimerBenchmark(timer, interval, runTime, toleranceInMilliseconds);
            benchmark.Run();

            Print(nameof(MultimediaTimer), benchmark);

            Assert.True(benchmark.Accuracy > 0.99d);

            // Findings:
            // - Average Deviation is less than MultimediaTimer
            // - Can set Interval to less than 1ms 
            // - Average Cpu Usage is higher than MultimediaTimer
        }

        [Fact]
        public void IsStopwatchPrecise()
        {
            var last = TimeSpan.Zero;
            var measures = new List<double>();

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.Seconds < 10)
            {
                var elapsed = (sw.Elapsed - last).TotalMilliseconds;
                measures.Add(elapsed);
                last = sw.Elapsed;
            }

            var precision = measures.Average();

            _output.WriteLine($"Precision: {precision:F5}ms.");

            Assert.True(precision < RequiredPrecisionInMilliseconds);
        }

        [Fact]
        public void IsHighResolutionDateTimePrecise()
        {
            var dateTime = new HighResolutionDateTime();
            DateTime last = dateTime.Now;
            var measures = new List<double>();

            var sw = Stopwatch.StartNew();
                        
            while (sw.Elapsed.Seconds < 10)
            {
                var elapsed = (dateTime.Now - last).TotalMilliseconds;
                measures.Add(elapsed);
                last = dateTime.Now;
            }

            var precision = measures.Average();

            _output.WriteLine($"Precision: {precision:F5}ms.");

            Assert.True(precision < RequiredPrecisionInMilliseconds);
        }

        private void Print(string name, TimerBenchmark benchmark)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"'{name}' Benchmark");
            builder.AppendLine();
            builder.AppendLine("Parameters:");
            builder.AppendLine($"Interval (ms): {benchmark.Interval.TotalMilliseconds:F2}");
            builder.AppendLine($"Run time (s): {benchmark.RunTime.TotalSeconds:F2}");
            builder.AppendLine($"Tolerance (ms): {benchmark.ToleranceInMilliseconds:F2}");
            builder.AppendLine();
            builder.AppendLine("Results:");
            builder.AppendLine($"Accuracy (%): {benchmark.Accuracy * 100:N3}");
            builder.AppendLine($"Average Deviation (ms): {benchmark.AverageDeviation:N3}");
            builder.AppendLine($"Max Deviation (ms): {benchmark.MaxDeviation:N3}");
            builder.AppendLine($"Min Deviation (ms): {benchmark.MinDeviation:N3}");
            builder.AppendLine($"Average Cpu Usage (%): {benchmark.AverageCpuUsed:N2}");
            builder.AppendLine($"Max Cpu Usage (%): {benchmark.MaxCpuUsed:N2}");
            builder.AppendLine($"Min Cpu Usage (%): {benchmark.MinCpuUsed:N2}");
            builder.AppendLine($"Average Memory Usage (Bytes): {benchmark.AverageMemoryUsed:N0}");
            builder.AppendLine($"Max Memory Usage (Bytes): {benchmark.MaxMemoryUsed:N0}");
            builder.AppendLine($"Min Memory Usage (Bytes): {benchmark.MinMemoryUsed:N0}");
            
            _output.WriteLine(builder.ToString());
        }
    }
}
