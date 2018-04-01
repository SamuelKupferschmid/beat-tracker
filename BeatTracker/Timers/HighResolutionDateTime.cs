namespace BeatTracker.Timers
{
    using System;
    using System.Runtime.InteropServices;

    // Source: https://manski.net/2014/07/high-resolution-clock-in-csharp/
    public class HighResolutionDateTime : IDateTime
    {
        public static bool IsAvailable { get; private set; }

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        public DateTime Now
        {
            get
            {
                if (!IsAvailable)
                {
                    throw new NotSupportedException(
                        "High resolution clock isn't available.");
                }

                long filetime;
                GetSystemTimePreciseAsFileTime(out filetime);

                return DateTime.FromFileTime(filetime);
            }
        }

        static HighResolutionDateTime()
        {
            try
            {
                long filetime;
                GetSystemTimePreciseAsFileTime(out filetime);
                IsAvailable = true;
            }
            catch (EntryPointNotFoundException)
            {
                // Not running Windows 8 or higher.
                IsAvailable = false;
            }
        }
    }
}
