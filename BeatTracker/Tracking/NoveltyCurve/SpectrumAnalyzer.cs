using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.Statistics;

namespace BeatTracker.Tracking.NoveltyCurve
{
    public class SpectrumAnalyzer
    {
        private readonly NoveltyParameters _parameters;
        private readonly float[] StftWindow;
        private readonly int StepSize;
        private readonly double compressionC = 1000;
        private readonly double resampleFeatureRate = 200;
        private readonly int _coefficientRangeEnd;
        private readonly int _diffLength;

        private readonly Queue<float[]> spectrumQueue;

        public SpectrumAnalyzer(NoveltyParameters parameters)
        {
            _parameters = parameters;
            int fftWindowSize = 1024;

            _diffLength = 0;
            this.StftWindow = MathNet.Numerics.Window.Hann(fftWindowSize).Select(Convert.ToSingle).ToArray();
            
            this._coefficientRangeEnd = (int)(_parameters.WindowLength / 2) + 1; //floor values
            
            var diffLength = (int)Math.Max(Math.Ceiling(0.3 * _parameters.FeatureRate), 5);
            diffLength = 2 * (int)Math.Round((double)diffLength / 2) + 1;

            spectrumQueue = new Queue<float[]>(Enumerable.Repeat(new float[_coefficientRangeEnd],(int)diffLength));

            var l = diffLength / 2;
            var filter = Enumerable.Repeat(-1, l).Concat(new[] {0}).Concat(Enumerable.Repeat(1, l));
            _diffFilter = Window.Hann(diffLength).Zip(filter, (f1, f2) => (float)f1 * f2).ToArray();
        }

        private float _maxSpectrumValue;
        private float[] _diffFilter;

        public float Next(float[] data)
        {
            var spec = StftSpectrogram(data);
            //normalize and convert to dB scale
            _maxSpectrumValue = Math.Max(_maxSpectrumValue, spec.Max());
            var threshold = (float)Math.Pow(10, -74d / 20);

            for (int i = 0; i < spec.Length; i++)
            {
                spec[i] = Math.Max((float)(spec[i] / _maxSpectrumValue), threshold);
            }

            var bands = new double[] { 0, 500, 1250, 3125, 7812.5, _parameters.DestinationSampleRate / 2 }; // in Hz
            var bandwiseNovelty = new List<float>();

            for (int i = 1; i < bands.Length; i++)
            {
                bandwiseNovelty.Add(calculcateBandNovelty(spec, bands[i - 1], bands[i]));
            }

            return (float) bandwiseNovelty.Mean();
        }

        private float[] StftSpectrogram(float[] data)
        {
            var ffTransoformed = new Complex32[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                ffTransoformed[i] = data[i] * StftWindow[i];
            }
            MathNet.Numerics.IntegralTransforms.Fourier.Forward(ffTransoformed);

            return ffTransoformed.Select(c => c.Magnitude * 26).Take(_coefficientRangeEnd).ToArray(); // divide by 26 to have similar values as Matlab implementation
        }

        private float calculcateBandNovelty(float[] spectrum, double bandStart, double bandEnd)
        {
            var binStart = (int)Math.Round(bandStart / _parameters.DestinationSampleRate * _parameters.WindowLength);
            var binEnd = (int)Math.Round(bandEnd / _parameters.DestinationSampleRate * _parameters.WindowLength);
            binEnd = Math.Min(_parameters.WindowLength / 2, binEnd);

            var bins = new float[binEnd - binStart];

            Array.Copy(spectrum, binStart, bins, 0, bins.Length);

            var logC = (float)Math.Log(1 + compressionC);
            for (int i = 0; i < bins.Length; i++)
            {
                bins[i] = (float)Math.Log(1 + bins[i] * compressionC) / logC;
            }


            return (float)spectrum.Mean();
        }
    }
}
