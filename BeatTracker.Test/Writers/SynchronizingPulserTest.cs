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

namespace BeatTracker.Test.Writers
{
    [Collection("Synchronizing")]
    public class SynchronizingPulserTest
    {
        private readonly ITestOutputHelper _output;

        public SynchronizingPulserTest(ITestOutputHelper output)
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
            var pulseReceiver = A.Fake<IPulseReceiver>();
            var writer = new SynchronizingPulser(tracker, timer, dateTime, pulseReceiver);

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 5)
                timer.Elapsed += Raise.WithEmpty();

            A.CallTo(() => pulseReceiver.OnPulse()).MustNotHaveHappened();
        }

        [Fact]
        public void DoesCallOnPulseWhenBeatInfoAvailable()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();
            var pulseReceiver = A.Fake<IPulseReceiver>();
            var writer = new SynchronizingPulser(tracker, timer, dateTime, pulseReceiver);

            tracker.BeatInfoChanged += Raise.With(new BeatInfo(100, dateTime.Now));

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 5)
                timer.Elapsed += Raise.WithEmpty();

            A.CallTo(() => pulseReceiver.OnPulse()).MustHaveHappened();
        }

        [Fact]
        public void DoesCallOnPulseWhenBeatInfoAvailableAndRemainsUnchanged()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();
            var pulseReceiver = A.Fake<IPulseReceiver>();
            var writer = new SynchronizingPulser(tracker, timer, dateTime, pulseReceiver);

            double bpm = 1000;

            tracker.BeatInfoChanged += Raise.With(new BeatInfo(bpm, dateTime.Now));

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 65)
            {
                timer.Elapsed += Raise.WithEmpty();
            }

            A.CallTo(() => pulseReceiver.OnPulse()).MustHaveHappened((int)bpm, Times.OrMore);
        }

        [Fact]
        public void DoesConsiderOffset()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();
            var pulseReceiver = A.Fake<IPulseReceiver>();
            var writer = new SynchronizingPulser(tracker, timer, dateTime, pulseReceiver);

            double bpm = 1000;
                        
            writer.Offset = TimeSpan.FromMilliseconds(150);

            Stopwatch sw = null;
            double measuredOffset = 0;

            bool isRunning = true;

            A.CallTo(() => pulseReceiver.OnPulse()).Invokes(call =>
            {
                measuredOffset = sw.ElapsedMilliseconds;
                isRunning = false;
            });
            
            tracker.BeatInfoChanged += Raise.With(new BeatInfo(bpm, dateTime.Now));

            sw = Stopwatch.StartNew();

            while (isRunning)
                timer.Elapsed += Raise.WithEmpty();

            var deviation = Math.Abs(measuredOffset - writer.Offset.TotalMilliseconds);

            _output.WriteLine($"Deviation: {deviation:F}ms.");

            Assert.True(deviation < TimerBenchmark.Tolerance);
        }

        [Fact]
        public void CalculatedInternalOffsetIsCorrect()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();
            var pulseReceiver = A.Fake<IPulseReceiver>();
            var writer = new SynchronizingPulser(tracker, timer, dateTime, pulseReceiver);

            double bpm = 1000;

            tracker.BeatInfoChanged += Raise.With(new BeatInfo(bpm, DateTime.Now));

            var simulatedWorkTimeSpan = TimeSpan.FromMilliseconds(50);

            A.CallTo(() => pulseReceiver.OnPulse()).Invokes(call => Thread.Sleep(simulatedWorkTimeSpan));

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 10)
            {
                timer.Elapsed += Raise.WithEmpty();
            }

            Assert.True(Math.Abs((simulatedWorkTimeSpan - writer.InternalOffset).TotalMilliseconds) < TimerBenchmark.Tolerance);
        }

        [Fact]
        public void DoesRecalculateInternalOffsetWhenOnPulseCallTimeVaries()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();
            var pulseReceiver = A.Fake<IPulseReceiver>();
            var writer = new SynchronizingPulser(tracker, timer, dateTime, pulseReceiver);

            var random = new Random();
            double bpm = 1000;

            tracker.BeatInfoChanged += Raise.With(new BeatInfo(bpm, DateTime.Now));

            var simulatedWorkTimeSpan = TimeSpan.FromMilliseconds(50);

            A.CallTo(() => pulseReceiver.OnPulse()).Invokes(call =>
            {
                Thread.Sleep(simulatedWorkTimeSpan);
            });

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 30)
            {
                timer.Elapsed += Raise.WithEmpty();
            }

            simulatedWorkTimeSpan = TimeSpan.FromMilliseconds(75);

            sw.Restart();
            while (sw.Elapsed.TotalSeconds < 30)
            {
                timer.Elapsed += Raise.WithEmpty();
            }

            Assert.True(Math.Abs((simulatedWorkTimeSpan - writer.InternalOffset).TotalMilliseconds) < TimerBenchmark.Tolerance);
        }

        [Fact]
        public void DoesBalanceOutWhenOnPulseCallsTakeTooLong()
        {
            var tracker = A.Fake<ITracker>();
            var timer = A.Fake<ITimer>();
            var dateTime = new HighResolutionDateTime();
            var pulseReceiver = A.Fake<IPulseReceiver>();
            var writer = new SynchronizingPulser(tracker, timer, dateTime, pulseReceiver);

            double bpm = 1000;

            tracker.BeatInfoChanged += Raise.With(new BeatInfo(bpm, DateTime.Now));

            // Simulate when work takes longer than bpm.
            var simulatedWorkTimeSpan = TimeSpan.FromMinutes(1 / bpm) + TimeSpan.FromMilliseconds(75);

            A.CallTo(() => pulseReceiver.OnPulse()).Invokes(async call => 
            {
                // Findings:

                // When OnPulse() implementation is 'non-blocking' or takes not a lot of time, then this Test will pass.
                // For e.g. implementation with 'await Task.Delay(simulatedWorkTimeSpan);'
                await Task.Delay(simulatedWorkTimeSpan);

                // When OnPulse() implementation is 'blocking' and takes a lot of time (more than 'TimeSpan.FromMinutes(1 / bpm)'), then this Test will fail.
                // For e.g. implementation with 'Thread.Sleep(simulatedWorkTimeSpan);'.
                
                // Thread.Sleep(simulatedWorkTimeSpan);
            });

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 65)
            {
                timer.Elapsed += Raise.WithEmpty();
            }

            A.CallTo(() => pulseReceiver.OnPulse()).MustHaveHappened((int)bpm, Times.OrMore);
        }
    }
}
