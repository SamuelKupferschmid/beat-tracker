using System;
using System.Threading;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using Sanford.Multimedia.Midi;

namespace BeatTracker.Writers
{
    public class MidiMetronomeWriter : SynchronizingWriter, IDisposable
    {
        protected readonly OutputDevice OutputDevice;

        public MidiMetronomeWriter(ITracker tracker)
            :this(tracker, 0) { }

        public MidiMetronomeWriter(ITracker tracker, int deviceId)
            : base(tracker)
        {
            OutputDevice = new OutputDevice(deviceId);
        }

        protected override void OnPulse()
        {
            ChannelMessageBuilder builder = new ChannelMessageBuilder
            {
                Command = ChannelCommand.NoteOn,
                MidiChannel = 9, // Drum Patch
                Data1 = 37,
                Data2 = 127
            };
            
            builder.Build();

            OutputDevice.Send(builder.Result);
        }

        public void Dispose()
        {
            OutputDevice?.Dispose();
        }
    }
}