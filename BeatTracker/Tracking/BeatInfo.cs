using System;

namespace BeatTracker.Tracking
{
    public class BeatInfo
    {
        public BeatInfo(double bpm, DateTime occursAt)
        {
            Bpm = bpm;
            OccursAt = occursAt;
        }

        public double Bpm { get; }

        /// <summary>
        /// Precise time at which the next beat occurs.
        /// </summary>
        public DateTime OccursAt { get; }
    }
}
