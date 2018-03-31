using BeatTracker.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BeatTracker.Test.Timers
{
    public class TimerTests
    {
        private const float MinimumAccuracy = 0.995f;

        private readonly ITestOutputHelper _output;

        public TimerTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void IsMultimediaTimerBenchmarkPassingRequirements()
        {
            var timer = new MultimediaTimer();            
            var benchmark = new TimerBenchmark(timer);
            benchmark.Run();

            Print(nameof(MultimediaTimer), benchmark);

            Assert.True(benchmark.Accuracy > MinimumAccuracy);
        }

        private void Print(string name, TimerBenchmark benchmark)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"'{name}' Benchmark Results:");
            builder.AppendLine($"Accuracy (%): {benchmark.Accuracy:N2}");
            builder.AppendLine($"Average Deviation (s): {benchmark.AverageDeviation:N3}");
            builder.AppendLine($"Max Deviation (s): {benchmark.MaxDeviation:N3}");
            builder.AppendLine($"Min Deviation (s): {benchmark.MinDeviation:N3}");
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
