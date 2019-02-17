using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Linq;

namespace BeatTracker.DFTPrototype.Utils
{
    public static class NoveltyCurve
    {
        public static float[] Preprocess(float[] data)
        {
            var spectrogram = Vector<float>.Build.DenseOfArray(data);

            // normalize
            var max = spectrogram.Max();
            spectrogram = spectrogram.Divide(max);

            // convert to dB
            var thresh = (float)Math.Pow(10, -74d / 20);
            spectrogram = spectrogram.PointwiseMaximum(thresh);

            // ToDo bandwide processing? - geht wahrsch. um die Gewichtung der Freq.-Bänder...
            var bands = new float[]
            {
                500, // von 0 Hz bis 500 Hz
                1250, // von 500 Hz bis 1250 Hz
                3125, // von 1250 Hz - 3125 Hz
                7812.5f, // von 3125 Hz - 7812.5 Hz
                11025 // von 7812.5 Hz - 11025 Hz
            };

            // log compression
            var compressionC = 1000f;
            spectrogram = spectrogram
                            .Multiply(compressionC)
                            .Add(1)
                            .PointwiseLog()
                            .Divide((float)Math.Log(1 + compressionC));

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

        public static double[] SmoothLocalAverageFilter(int sampleRate, int stepSize)
        {
            var localAverageDuration = TimeSpan.FromSeconds(1.5d);
            var localAverageWindowLength = (int)Math.Max(Math.Ceiling(localAverageDuration.TotalSeconds * sampleRate / stepSize), 3);
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
    }
}
