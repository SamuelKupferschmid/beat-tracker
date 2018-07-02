using BeatTracker.Helpers;
using BeatTracker.Test.Timers;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using BeatTracker.Writers;
using FakeItEasy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using EasyAssertions;

namespace BeatTracker.Test.Writers
{
    [Collection("Synchronizing")]
    public class SynchronizingWriterTest
    {
        private const double ToleranceInMilliseconds = 5.0d;

        private readonly ITestOutputHelper _output;

        public SynchronizingWriterTest(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));

            ProcessPriority.SetCurrentProcessPriorityToHigh();
        }

        [Fact]
        public void DoesNotCallOnPulseWhenBeatInfoNotAvailable()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();
            var writer = new MockSynchronizingWriter(tracker, timer, dateTime);

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 5)
            {
                timer.Elapsed += Raise.WithEmpty();
                Thread.Sleep(1);
            }

            writer.OnPulseActionCallCount.ShouldBe(0);
        }

        [Fact]
        public void DoesCallOnPulseWhenBeatInfoAvailable()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();
            var writer = new MockSynchronizingWriter(tracker, timer, dateTime);

            tracker.BeatInfoChanged += Raise.With(new BeatInfo(100, dateTime.Now, 1));

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 5)
            {
                timer.Elapsed += Raise.WithEmpty();
                Thread.Sleep(1);
            }

            writer.OnPulseActionCallCount.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void DoesCallOnPulseWhenBeatInfoAvailableAndRemainsUnchanged()
        {
            int runTimeInSeconds = 65;
            var acceptableDeviationTolerance = 0.02d; // 2%

            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();
            var writer = new MockSynchronizingWriter(tracker, timer, dateTime);

            double bpm = 1000;

            tracker.BeatInfoChanged += Raise.With(new BeatInfo(bpm, dateTime.Now, 1));

            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < runTimeInSeconds)
            {
                timer.Elapsed += Raise.WithEmpty();
                Thread.Sleep(1);
            }

            var theoreticalOnPulseCallCount = (runTimeInSeconds / TimeSpan.FromMinutes(1 / bpm).TotalSeconds);

            var deviation = Math.Abs(writer.OnPulseActionCallCount - theoreticalOnPulseCallCount) / theoreticalOnPulseCallCount;

            _output.WriteLine($"Expected Call Count: {theoreticalOnPulseCallCount}");
            _output.WriteLine($"Actual Call Count: {writer.OnPulseActionCallCount}");
            _output.WriteLine($"Deviation: {deviation:F5}");

            deviation.ShouldBeLessThan(acceptableDeviationTolerance);
        }

        [Fact]
        public void DoesConsiderOffset()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();

            double bpm = 1000;
            Stopwatch sw = null;
            double measuredOffset = 0;
            bool isRunning = true;
            
            var onPulseAction = new Action(() =>
            {
                measuredOffset = sw.Elapsed.TotalMilliseconds;
                isRunning = false;
            });

            var writer = new MockSynchronizingWriter(tracker, timer, dateTime, onPulseAction);                                    
            writer.Offset = TimeSpan.FromMilliseconds(150);
                        
            tracker.BeatInfoChanged += Raise.With(new BeatInfo(bpm, dateTime.Now, 1));

            sw = Stopwatch.StartNew();

            while (isRunning)
            {
                timer.Elapsed += Raise.WithEmpty();
                Thread.Sleep(1);
            }

            var deviation = Math.Abs(measuredOffset - writer.Offset.TotalMilliseconds);

            deviation.ShouldBeLessThan(ToleranceInMilliseconds);
        }

        [Fact]
        public void IsCalculatedOnPulseDurationCorrect()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();

            double bpm = 1000;
            var simulatedWorkTimeSpan = TimeSpan.FromMilliseconds(50);

            var onPulseAction = new Action(() =>
            {
                Thread.Sleep(simulatedWorkTimeSpan);
            });

            var writer = new MockSynchronizingWriter(tracker, timer, dateTime, onPulseAction);
            
            tracker.BeatInfoChanged += Raise.With(new BeatInfo(bpm, dateTime.Now, 1));

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 10)
            {
                timer.Elapsed += Raise.WithEmpty();
                Thread.Sleep(1);
            }

            var deviation = Math.Abs((simulatedWorkTimeSpan - writer.AverageOnPulseDuration).TotalMilliseconds);

            _output.WriteLine($"Simulated Work: {simulatedWorkTimeSpan}");
            _output.WriteLine($"AverageOnPulseDuration: {writer.AverageOnPulseDuration}");
            _output.WriteLine($"Deviation: {deviation:F3}ms.");

            deviation.ShouldBeLessThan(ToleranceInMilliseconds);
        }

        [Fact]
        public void DoesRecalculateOnPulseDuration()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();

            var simulatedWorkTimeSpan = TimeSpan.FromMilliseconds(50);
            var onPulseAction = new Action(() =>
            {
                Thread.Sleep(simulatedWorkTimeSpan);
            });

            var writer = new MockSynchronizingWriter(tracker, timer, dateTime, onPulseAction);

            var random = new Random();
            double bpm = 1000;

            tracker.BeatInfoChanged += Raise.With(new BeatInfo(bpm, dateTime.Now, 1));

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 30)
            {
                timer.Elapsed += Raise.WithEmpty();
                Thread.Sleep(1);
            }

            simulatedWorkTimeSpan = TimeSpan.FromMilliseconds(75);

            sw.Restart();
            while (sw.Elapsed.TotalSeconds < 30)
            {
                timer.Elapsed += Raise.WithEmpty();
                Thread.Sleep(1);
            }

            var deviation = Math.Abs((simulatedWorkTimeSpan - writer.AverageOnPulseDuration).TotalMilliseconds);

            _output.WriteLine($"Simulated Work: {simulatedWorkTimeSpan}");
            _output.WriteLine($"AverageOnPulseDuration: {writer.AverageOnPulseDuration}");
            _output.WriteLine($"Deviation: {deviation:F3}ms.");

            deviation.ShouldBeLessThan(ToleranceInMilliseconds);
        }

        [Fact]
        public void DoesBalanceOutWhenOnPulseCallsTakeTooLong()
        {
            int runTimeInSeconds = 65;
            var acceptableDeviationTolerance = 0.05d; // 5%

            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();

            double bpm = 1000;

            // Simulate when work takes longer than bpm.
            var simulatedWorkTimeSpan = TimeSpan.FromMinutes(1 / bpm) + TimeSpan.FromMilliseconds(75);

            var onPulseAction = new Action(async () =>
            {
                // Findings:

                // 1. When OnPulse() implementation is 'non-blocking' or takes not a lot of time, then this Test will pass.
                // For e.g. implementation with 'await Task.Delay(simulatedWorkTimeSpan);'
                await Task.Delay(simulatedWorkTimeSpan);

                // 2. When OnPulse() implementation is 'blocking' and takes a lot of time (more than 'TimeSpan.FromMinutes(1 / bpm)'), then this Test will fail.
                // For e.g. implementation with 'Thread.Sleep(simulatedWorkTimeSpan);'.
                // Thread.Sleep(simulatedWorkTimeSpan);
            });

            var writer = new MockSynchronizingWriter(tracker, timer, dateTime, onPulseAction);

            tracker.BeatInfoChanged += Raise.With(new BeatInfo(bpm, dateTime.Now, 1));

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < runTimeInSeconds)
            {
                timer.Elapsed += Raise.WithEmpty();
                Thread.Sleep(1);
            }
            
            var theoreticalOnPulseCallCount = (runTimeInSeconds / TimeSpan.FromMinutes(1 / bpm).TotalSeconds);

            var deviation = Math.Abs(writer.OnPulseActionCallCount - theoreticalOnPulseCallCount) / theoreticalOnPulseCallCount;

            _output.WriteLine($"Expected Call Count: {theoreticalOnPulseCallCount}");
            _output.WriteLine($"Actual Call Count: {writer.OnPulseActionCallCount}");
            _output.WriteLine($"Deviation: {deviation:F5}");

            deviation.ShouldBeLessThan(acceptableDeviationTolerance);
        }
    }
}
