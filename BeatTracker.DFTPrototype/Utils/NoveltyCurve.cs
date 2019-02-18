using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatTracker.DFTPrototype.Utils
{
    public static class NoveltyCurve
    {
        public static float[] Preprocess(int sampleRate, float[] data)
        {
            var spectrogram = new float[data.Length];

            // normalize
            var max = data.Max();
            max = 1;

            // convert to dB
            var thresh = (float)Math.Pow(10, -74d / 20);

            // set band weights
            var bands = new List<(float fromFreq, float toFreq, float weight)>()
            {
                (0, 500, 1),
                (500, 1250, 1),
                (1250, 3125, 1),
                (3125, 7812.5f, 1),
                (7812.5f, 11025, 1),
                (11025, 22050, 1)
            };
            
            // log compression
            var compressionC = 1000f;

            var freqToIndex = (float)data.Length / (sampleRate / 2);

            foreach (var band in bands)
            {
                if (band.toFreq <= sampleRate / 2)
                {
                    var start = (int)Math.Ceiling(freqToIndex * band.fromFreq);
                    var end = (int)Math.Ceiling(freqToIndex * band.toFreq);

                    for (int i = start; i < end; i++)
                    {
                        var value = Math.Max(data[i] / max, thresh);
                        spectrogram[i] = (float)(Math.Log((value * compressionC * band.weight) + 1) / Math.Log(1 + compressionC * band.weight));
                    }
                }
            }

            return spectrogram.ToArray();
        }

        public static double[] SmoothNormalizeFilter(int sampleRate, int stepSize)
        {
            var normalizeDuration = TimeSpan.FromSeconds(5.0d);
            var normalizeWindowLength = (int)Math.Max(Math.Ceiling(normalizeDuration.TotalSeconds * sampleRate / stepSize), 3);
            var normalizeFilter = Window.Hann(normalizeWindowLength);
            var normalizeFilterSum = normalizeFilter.Sum();
            for (int i = 0; i < normalizeFilter.Length; i++)
                normalizeFilter[i] /= normalizeFilterSum;

            return normalizeFilter;
        }

        public static double[] SmoothDifferentiateFilter(int sampleRate, int stepSize)
        {
            var smoothedDiffDuration = TimeSpan.FromSeconds(0.3d);
            var smoothedDiffWindowLength = (int)Math.Max(Math.Ceiling(smoothedDiffDuration.TotalSeconds * sampleRate / stepSize), 5);
            smoothedDiffWindowLength = (int)((2 * Math.Ceiling(smoothedDiffWindowLength / 2d)) + 1);
            var smoothedDiffFilter = Window.Hann(smoothedDiffWindowLength);
            for (int i = 0; i < smoothedDiffWindowLength; i++)
            {
                if (i < smoothedDiffWindowLength / 2)
                {
                    smoothedDiffFilter[i] *= -1;
                }
            }

            return smoothedDiffFilter;
        }

        public static double[] SmoothLocalAverageFilter(int featureRate)
        {
            var localAverageDuration = TimeSpan.FromSeconds(1.5d);
            var localAverageWindowLength = (int)Math.Max(Math.Ceiling(localAverageDuration.TotalSeconds * featureRate), 3);
            var localAverageFilter = Window.Hann(localAverageWindowLength);
            var localAverageFilterSum = localAverageFilter.Sum();
            for (int i = 0; i < localAverageFilter.Length; i++)
                localAverageFilter[i] /= localAverageFilterSum;

            return localAverageFilter;
        }

        public static void ApplyFilter(float[] data, double[] window, bool discardNegativeValues)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (float)(data[i] * window[i]);

                if (discardNegativeValues && data[i] < 0)
                    data[i] = 0;
            }
        }

        public static float[] Resample(float[] data, int samples)
        {
            var x = Enumerable.Range(0, data.Length).Select(i => (double)i).ToArray();
            var y = data.Select(f => (double)f).ToArray();

            //var interpolate = CubicSpline.InterpolateAkimaSorted(x, y);
            var interpolate = LinearSpline.InterpolateSorted(x, y);

            var resampledData = new float[samples];
            var step = ((float)data.Length / resampledData.Length);

            for (int i = 0; i < resampledData.Length; i++)
            {
                var t = i * step;
                resampledData[i] = (float)interpolate.Interpolate(t);
            }

            return resampledData;
        }
    }
}
