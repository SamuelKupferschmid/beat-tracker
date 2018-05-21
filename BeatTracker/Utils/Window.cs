using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Utils
{
    public static class Window
    {
        public static IList<double> CreateHann(int n)
        {
            var cosFactor = 2 * Math.PI / (n - 1);
            return Enumerable.Range(0, n - 1).Select(v => 0.5 - 0.5 * Math.Cos(cosFactor * n)).ToList();
        }
    }
}
