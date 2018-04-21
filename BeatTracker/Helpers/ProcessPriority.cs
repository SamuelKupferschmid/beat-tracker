using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Helpers
{
    public static class ProcessPriority
    {
        public static bool SetCurrentProcessPriority(ProcessPriorityClass priorityClass)
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();

                if (process.PriorityClass != priorityClass)
                    process.PriorityClass = priorityClass;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetCurrentProcessPriorityToHigh()
        {
            return SetCurrentProcessPriority(ProcessPriorityClass.High);
        }
    }
}
