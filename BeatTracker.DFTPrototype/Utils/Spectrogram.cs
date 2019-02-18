using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.DFTPrototype.Utils
{
    public static class Spectrogram
    {
        public static Complex[] ToComplexSpectrogram(float[] data)
        {
            var window = Window.Hann(data.Length);

            var complexData = Enumerable.Range(0, data.Length)
                .Select(i => new Complex(data[i] * (float)window[i], 0))
                .ToArray();

            Fourier.Forward(complexData, FourierOptions.Matlab);

            var bins = (data.Length / 2) + 1;

            //var bins = (int)Math.Ceiling(data.Length / 2f);

            return complexData.Take(bins).ToArray();
        }

        public static float[] ToMagnitudeSpectrogram(float[] data)
        {
            var complexData = ToComplexSpectrogram(data);
            return complexData.Select(c => (float)c.Magnitude).ToArray();
        }
    }
}
