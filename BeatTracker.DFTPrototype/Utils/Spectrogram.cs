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
        public static float[] ToMagnitudeSpectrogram(float[] data, double[] window)
        {
            var complexData = Enumerable.Range(0, data.Length)
                .Select(i => new Complex(data[i] * (float)window[i], 0))
                .ToArray();

            Fourier.Forward(complexData, FourierOptions.Matlab);

            // consider only first half of fft
            var bins = (data.Length / 2) + 1;
            complexData = complexData.Take(bins).ToArray(); 

            return complexData.Select(c => (float)c.Magnitude).ToArray();
        }
    }
}
