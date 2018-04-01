using System;
using System.Threading;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using Sanford.Multimedia.Midi;

namespace BeatTracker.Writers
{
    class MidiMetronomeWriter : IPulseReceiver, IDisposable
    {
        protected readonly OutputDevice OutputDevice;

        public MidiMetronomeWriter()
        {
            OutputDevice = new OutputDevice(0);
        }

        public void OnPulse()
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

            Thread.Sleep(1000);

            builder.Command = ChannelCommand.NoteOff;
            builder.Data2 = 0;
            builder.Build();


            OutputDevice.Send(builder.Result);
        }

        public void Dispose()
        {
            OutputDevice?.Dispose();
        }
    }
}