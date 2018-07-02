using System;

namespace BeatTracker.Tracking
{
    public class BeatInfo
    {
        public BeatInfo(double bpm, DateTime occursAt, double confidence)
        {
            Bpm = bpm;
            OccursAt = occursAt;
            Confidence = confidence;
        }

        public double Bpm { get; }

        /// <summary>
        /// Precise time at which the next beat occurs.
        /// </summary>
        public DateTime OccursAt { get; }

        public double Confidence { get; }
    }
}
