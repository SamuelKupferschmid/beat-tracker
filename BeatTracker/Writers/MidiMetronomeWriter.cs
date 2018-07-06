using System;
using System.Threading;
using System.Threading.Tasks;
using BeatTracker.Timers;
using BeatTracker.Tracking;
using BeatTracker.Utils;
using Sanford.Multimedia.Midi;

namespace BeatTracker.Writers
{
    public class MidiMetronomeWriter : SynchronizingWriter, IDisposable
    {
        protected OutputDevice OutputDevice;

        private readonly ChannelMessageBuilder kick;
        private readonly ChannelMessageBuilder snare;
        private readonly ChannelMessageBuilder hihat;

        private readonly bool[] _kickNotes = new []{true, false, false, false, true, false, false, false};
        private readonly bool[] _snarekNotes = new []{false, false, true, false, false, false, true, false};
        private readonly bool[] _hihatNotes = new []{false, true, false, true, false, true, false, true };

        public MidiMetronomeWriter(ITracker tracker) : this(tracker, 0)
        {
        }

        public MidiMetronomeWriter(ITracker tracker, int deviceId)
            : base(tracker)
        {
            OutputDevice = new OutputDevice(deviceId);

            this.kick = new ChannelMessageBuilder
            {
                Command = ChannelCommand.NoteOn,
                MidiChannel = 9, // Drum Patch
                Data1 = 36,
                Data2 = 127
            };
            this.snare = new ChannelMessageBuilder
            {
                Command = ChannelCommand.NoteOn,
                MidiChannel = 9, // Drum Patch
                Data1 = 38,
                Data2 = 127
            };
            this.hihat = new ChannelMessageBuilder
            {
                Command = ChannelCommand.NoteOn,
                MidiChannel = 9, // Drum Patch
                Data1 = 42,
                Data2 = 127
            };

            this.kick.Build();
            this.snare.Build();
            this.hihat.Build();
        }

        private int _index = 0;

        protected override async void OnPulse(BeatInfo info)
        {
            if (!OutputDevice.IsDisposed)
            {
                OutputDevice?.Send(this.hihat.Result);
            }

            //int volume = (int)(info.Confidence - 80 * 0.3).Clamp(0, 127.999);

            //this.kick.Data2 = volume;
            //this.snare.Data2 = volume;
            //this.hihat.Data2 = volume;
            //this.kick.Build();
            //this.snare.Build();
            //this.hihat.Build();

            //if (_kickNotes[mod])
            //   OutputDevice.Send(this.kick.Result);
            //if (_snarekNotes[mod])
            //    OutputDevice.Send(this.snare.Result);
            //if (_hihatNotes[mod])
            //   OutputDevice.Send(this.hihat.Result);
        }

        public void Dispose()
        {
            OutputDevice?.Dispose();
        }
    }
}