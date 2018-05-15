using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Utils
{
    public static class SignalProcessing
    {
        public static IEnumerable<float> Resample(IEnumerable<float> source, int p, int q)
        {
            (p, q) = Ratio(p, q);
            int i = 0;
            bool first = true;
            float prev = 0;
            foreach (var next in source)
            {
                if (first)
                {
                    prev = next;
                    first = false;
                    continue;
                }

                do
                {
                    if (i % q == 0)
                    {
                        float v = ((i % p) / (float)p);
                        yield return prev + (next - prev) * v;
                    }
                } while (++i % p != 0);

                prev = next;
            }
        }

        public static (int a, int b) Ratio(int a, int b)
        {
            var gcd = (int)MathNet.Numerics.Euclid.GreatestCommonDivisor(a, b);
            return (a / gcd, b / gcd);
        }
    }
}
