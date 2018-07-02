using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using BeatTracker.Utils;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra;

namespace BeatTracker.Tracking
{
    /// <summary>
    /// Extracts periodic patterns
    /// </summary>
    public class PulseAnalyzer
    {
        private readonly BufferedTransformer _transformer;
        private readonly float[] _buffer = new float[1];

        private readonly SpectrumLogger _outputLogger = SpectrumLogger.Create("PulseAnalyzer");

        private float[] _bpmFrequencies;

        private double[] _window;
        private double _windowNormalizeFactor;

        private readonly float _featureRate;

        public PulseAnalyzer()
        {
            // ** Initialize BPM Frequency Range

            var minBpm = 30;
            var maxBpm = 600;

            _bpmFrequencies = new float[maxBpm - minBpm];

            for (int i = minBpm; i < maxBpm; i++)
            {
                _bpmFrequencies[i - minBpm] = i / 60.0f;
            }

            // ** Initialize BufferedTransformer

            var audioSampleRate = 22050; // Mono 22.05 KHz
            var frequencyAnalyzerStepSize = 512;
            _featureRate = audioSampleRate / (float)frequencyAnalyzerStepSize;

            //_featureRate = 200;
            var fftWindowSize = 6 * _featureRate; // 6 Seconds
            var fftStepSize = Math.Ceiling(_featureRate / 5);

            _transformer = new BufferedTransformer((int)fftWindowSize, (int)fftStepSize, Transform_via_DFT_v2);
            _transformer.FrameAvailable += _transformer_FrameAvailable;

            // ** Initialize Window Function

            _window = MathNet.Numerics.Window.Hann(_transformer.WindowSize);
            _windowNormalizeFactor = Math.Sqrt(_transformer.WindowSize) / _window.Sum() * _transformer.WindowSize;
        }

        private float[] Transform_via_DFT_v1(float[] data)
        {
            var transformedVector = Vector<float>.Build.Dense(_bpmFrequencies.Length);

            for (int f = 0; f < _bpmFrequencies.Length; f++)
            {
                var twoPiFreq = 2 * Math.PI * _bpmFrequencies[f];

                var cosValue = 0d;
                var sinValue = 0d;

                for (int s = 0; s < data.Length; s++)
                {
                    var timedValue = (s + 1) / _featureRate;
                    var cosFunc = Math.Cos(twoPiFreq * timedValue);
                    var sinFunc = Math.Sin(twoPiFreq * timedValue);

                    cosValue += (data[s] * _window[s] * cosFunc);
                    sinValue += (data[s] * _window[s] * sinFunc);
                }

                var magnitude = new Complex(cosValue, sinValue).Magnitude;

                transformedVector[f] = (float)(magnitude * _windowNormalizeFactor);
            }

            transformedVector = transformedVector.Normalize(2);

            return transformedVector.AsArray();
        }

        private Matrix<float> _DFT_Cos_Matrix;
        private Matrix<float> _DFT_Sin_Matrix;

        private float[] Transform_via_DFT_v2(float[] data)
        {
            if (_DFT_Cos_Matrix == null
                || _DFT_Sin_Matrix == null)
            {
                _DFT_Cos_Matrix = Matrix<float>.Build.Dense(_bpmFrequencies.Length, _transformer.WindowSize);
                _DFT_Sin_Matrix = Matrix<float>.Build.Dense(_bpmFrequencies.Length, _transformer.WindowSize);

                var window = MathNet.Numerics.Window.Hann(_transformer.WindowSize);

                for (int i = 0; i < _bpmFrequencies.Length; i++)
                {
                    var twoPiFreq = 2 * Math.PI * _bpmFrequencies[i];

                    for (int j = 0; j < _transformer.WindowSize; j++)
                    {
                        var timedValue = (j + 1) / _featureRate;
                        var cosFunc = Math.Cos(twoPiFreq * timedValue);
                        var sinFunc = Math.Sin(twoPiFreq * timedValue);

                        _DFT_Cos_Matrix.At(i, j, (float)(cosFunc * window[j]));
                        _DFT_Sin_Matrix.At(i, j, (float)(sinFunc * window[j]));
                    }
                }
            }
                        
            var dataVector = Vector<float>.Build.DenseOfArray(data);

            var cosValues = _DFT_Cos_Matrix * dataVector;
            var sinValues = _DFT_Sin_Matrix * dataVector;
                        
            var transformedVector = Vector<float>.Build.Dense(_bpmFrequencies.Length);
            for (int i = 0; i < _bpmFrequencies.Length; i++)
            {
                var magnitude = new Complex(cosValues[i], sinValues[i]).Magnitude;
                transformedVector[i] = (float)(magnitude * _windowNormalizeFactor);
            }

            transformedVector = transformedVector.Normalize(2);

            return transformedVector.AsArray();
        }

        private float[] Transform_via_ACF_v1(float[] data)
        {


            return data;
        }

        public event EventHandler<IEnumerable<(float bpm, float confidence)>> PulseExtracted;

        private void _transformer_FrameAvailable(object sender, float[] e)
        {
            var index = GetMaxFreqIndex(e);
            var confidence = e[index];
            var bpm = _bpmFrequencies[index] * 60;

            //bpm /= 2.32f;

            bpm /= 4;

            while (bpm < 160)
            {
                bpm *= 2;
            }
#if DEBUG
            System.Diagnostics.Debug.Print($"BPM: {bpm:F} | Confidence: {confidence:F5}");
#endif

            _outputLogger.AddSampe(e);

            PulseExtracted?.Invoke(this, new[]
            {
                ((float)bpm, (float)confidence)
            });
        }

        public void AddFrame(float f)
        {
            _buffer[0] = f;
            _transformer.AddSamples(new WaveSamples(_buffer,_buffer.Length));
        }

        private int GetMaxFreqIndex(float[] e)
        {
            int freqIndex = 0;
            float confidence = 0;

            for (int i = 0; i < e.Length; i++)
            {
                if (e[i] > confidence)
                {
                    freqIndex = i;
                    confidence = e[i];
                }
            }

            return freqIndex;
        }
    }
}
