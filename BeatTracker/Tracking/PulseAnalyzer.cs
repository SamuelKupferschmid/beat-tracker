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
        
        private readonly int _featureRate;

        public PulseAnalyzer(int featureRate)
        {
            //var audioSampleRate = 22050; // Mono 22.05 KHz
            //var frequencyAnalyzerStepSize = 512;
            //_featureRate = audioSampleRate / (float)frequencyAnalyzerStepSize;
            _featureRate = featureRate;
            var fftWindowSize = 6 * _featureRate; // 6 Seconds
            var fftStepSize = 40;

            var minBpm = 30;
            var maxBpm = 600;

            _bpmFrequencies = new float[maxBpm - minBpm];

            for (int i = minBpm; i < maxBpm; i++)
            {
                _bpmFrequencies[i - minBpm] = i / 60.0f;
            }

            _transformer = new BufferedTransformer((int)fftWindowSize, (int)fftStepSize, Transform_via_DFT_v2);
            _transformer.FrameAvailable += _transformer_FrameAvailable;
        }

        private float[] Transform_via_DFT_v1(float[] data)
        {
            var transformed = new float[_bpmFrequencies.Length];

            var window = MathNet.Numerics.Window.Hann(_transformer.WindowSize);

            for (int f = 0; f < transformed.Length; f++)
            {
                var twoPiFreq = 2 * Math.PI * _bpmFrequencies[f];

                var cosValue = 0d;
                var sinValue = 0d;

                for (int s = 0; s < data.Length; s++)
                {
                    var timedValue = (s + 1) / _featureRate;
                    var cosFunc = Math.Cos(twoPiFreq * timedValue);
                    var sinFunc = Math.Sin(twoPiFreq * timedValue);

                    cosValue += (data[s] * window[s] * cosFunc);
                    sinValue += (data[s] * window[s] * sinFunc);
                }


                var complex = new Complex(cosValue, sinValue);
                var value = (float)complex.Magnitude;

                transformed[f] = value;
            }

            return transformed;
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
                        
            var transformed = new float[_bpmFrequencies.Length];
            for (int i = 0; i < _bpmFrequencies.Length; i++)
                transformed[i] = (float)new Complex(cosValues[i], sinValues[i]).Magnitude;

            return transformed;
        }

        public event EventHandler<IEnumerable<(float bpm, float confidence)>> PulseExtracted;

        private void _transformer_FrameAvailable(object sender, float[] e)
        {
            var index = GetMaxFreqIndex(e);
            var confidence = e[index];
            var bpm = _bpmFrequencies[index] * 60;

            System.Diagnostics.Debug.Print($"BPM: {bpm:F} | Confidence: {confidence:F5}");

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
            float confidence = float.MinValue;

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
