using BeatTracker.Tracking;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Linq;

namespace BeatTracker.DFTPrototype.Utils
{
    public class Tempogram
    {
        public const int DefaultMinBpm = 30;
        public const int DefaultMaxBpm = 600;

        private readonly int _bufferSize;
        private readonly int _stepSize;
        private readonly float _featureRate;

        private float[] _bpmFrequencies;

        private double[] _windowPLP;
        private float _windowNormalizeFactor;

        private Matrix<float> _DFT_Cos_Matrix;
        private Matrix<float> _DFT_Sin_Matrix;

        public Tempogram(int bufferSize, int stepSize, float featureRate, int minBpm = DefaultMinBpm, int maxBpm = DefaultMaxBpm)
        {
            _bufferSize = bufferSize;
            _stepSize = stepSize;
            _featureRate = featureRate;

            _bpmFrequencies = new float[maxBpm - minBpm];

            for (int i = minBpm; i < maxBpm; i++)
            {
                _bpmFrequencies[i - minBpm] = i / 60.0f;
            }

            _DFT_Cos_Matrix = Matrix<float>.Build.Dense(_bpmFrequencies.Length, bufferSize);
            _DFT_Sin_Matrix = Matrix<float>.Build.Dense(_bpmFrequencies.Length, bufferSize);

            var window = Window.Hann(bufferSize);
            _windowNormalizeFactor = (float)((1 / Math.Sqrt(bufferSize)) / window.Sum() * bufferSize);

            for (int i = 0; i < _bpmFrequencies.Length; i++)
            {
                var twoPiFreq = 2 * Math.PI * _bpmFrequencies[i];

                for (int j = 0; j < bufferSize; j++)
                {
                    var timedValue = (j + 1) / featureRate;
                    var cosFunc = Math.Cos(twoPiFreq * timedValue);
                    var sinFunc = Math.Sin(twoPiFreq * timedValue);

                    _DFT_Cos_Matrix.At(i, j, (float)(cosFunc * window[j]));
                    _DFT_Sin_Matrix.At(i, j, (float)(sinFunc * window[j]));
                }
            }

            _windowPLP = Window.Hann(bufferSize);
            var sumWindowPLP = _windowPLP.Sum();
            for (int i = 0; i < _windowPLP.Length; i++)
            {
                _windowPLP[i] = (_windowPLP[i] / (sumWindowPLP / bufferSize)) / (bufferSize / stepSize);
            }
        }

        public int BufferSize => _bufferSize;

        public int StepSize => _stepSize;

        public float FeatureRate => _featureRate;

        public Complex32[] TempogramViaDFT(float[] data)
        {
            var dataVector = Vector<float>.Build.DenseOfArray(data);

            var cosValues = _DFT_Cos_Matrix * dataVector;
            var sinValues = _DFT_Sin_Matrix * dataVector;

            var tempogram = new Complex32[_bpmFrequencies.Length];
            for (int i = 0; i < _bpmFrequencies.Length; i++)
            {
                tempogram[i] = new Complex32(cosValues[i], sinValues[i]) * _windowNormalizeFactor;
            }

            return tempogram;
        }

        public BeatInfo BestBpm(Complex32[] data)
        {
            int bestIndex = -1;
            Complex32 bestValue = Complex32.Zero;

            for (int i = 0; i < data.Length; i++)
            {
                if (bestIndex < 0
                    || data[i].Magnitude > bestValue.Magnitude)
                {
                    bestIndex = i;
                    bestValue = data[i];
                }
            }

            return new BeatInfo(_bpmFrequencies[bestIndex] * 60, DateTime.MaxValue, bestValue.Magnitude);
        }

        public float[] PLPCurve(Complex32[] data)
        {
            int bestIndex = -1;
            Complex32 bestValue = Complex32.Zero;

            for (int i = 0; i < data.Length; i++)
            {
                if (bestIndex < 0
                    || data[i].Magnitude > bestValue.Magnitude)
                {
                    bestIndex = i;
                    bestValue = data[i];
                }
            }

            var phase = bestValue.Phase;
            var tPeriod = _featureRate / _bpmFrequencies[bestIndex];

            var cosine = new float[_bufferSize];
            for (int i = 0; i < cosine.Length; i++)
                cosine[i] = (float)Math.Max(_windowPLP[i] * Math.Cos(i / tPeriod * 2 * Math.PI + phase), 0);

            return cosine;
        }
    }
}
