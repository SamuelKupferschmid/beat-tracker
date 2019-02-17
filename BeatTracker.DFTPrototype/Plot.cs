using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.DFTPrototype
{
    public static class Plot
    {
        public static event Action<float[]> Show;

        public static void Export(float[] points)
        {
            File.WriteAllText("Export-Points.csv", string.Join(Environment.NewLine, points.Select(x => $"{x:F6}").ToArray()));
        }
    }
}
