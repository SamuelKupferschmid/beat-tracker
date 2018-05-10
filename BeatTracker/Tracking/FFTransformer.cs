using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using MathNet.Numerics.IntegralTransforms;

namespace BeatTracker.Tracking
{
    public class FFTransformer
    {
        private readonly int _bins;
        private float[] _equalizer;
        private Func<int, float> _equalizerFunc;

        private int _coefficientRangeMin;
        private int _coefficientRangeMax;

        private readonly BufferedTransformer _transformer;

        public FFTransformer(int windowSize)
            : this(windowSize, windowSize)
        {
        }

        public FFTransformer(int windowSize, int stepSize)
            : this(windowSize, stepSize, 0, windowSize / 2)
        {
        }

        public FFTransformer(int windowSize, int stepSize, int coefficientRangeMin, int coefficientRangeMax)
        {
            _transformer = new BufferedTransformer(windowSize, stepSize, Transform_via_FFT);
            _transformer.FrameAvailable += (o, e) => FrameAvailable?.Invoke(this, e);

            if (coefficientRangeMin < 0)
                throw new ArgumentException(nameof(coefficientRangeMin));

            if (coefficientRangeMax < 0 || coefficientRangeMax < coefficientRangeMin)
                throw new ArgumentException(nameof(coefficientRangeMax));

            _coefficientRangeMin = coefficientRangeMin;
            _coefficientRangeMax = coefficientRangeMax;
            
            _bins = windowSize / 2;

            _equalizer = Enumerable.Repeat(1f, windowSize).ToArray();
        }

        private float[] Transform_via_FFT(float[] data)
        {
            //var complexData = new Complex[data.Length];
            //for (int i = 0; i < data.Length; i++)
            //{
            //    complexData[i] = new Complex(data[i], 0);
            //}

            //Fourier.Forward(complexData);

            //for (int i = 0; i < data.Length; i++)
            //{
            //    data[i] = (float)complexData[i].Magnitude;
            //}

            if (UseWindow)
            {
                var window = MathNet.Numerics.Window.Hann(data.Length);

                for (var i = 0; i  < data.Length; i++)
                {
                    data[i] *= (float)window[i];
                }
            }

            Fourier.ForwardReal(data, data.Length - 1);

            if (_equalizerFunc != null)
            {
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] *= _equalizer[i];
                }
            }

            int range = _coefficientRangeMax - _coefficientRangeMin;
            if (range < data.Length)
            {
                var rangedata = new float[range];
                Array.Copy(data, _coefficientRangeMin, rangedata, 0, range);
                data = rangedata;
            }

            return data;
        }

        public Func<int, float> EqualizerFunc
        {
            get => _equalizerFunc;
            set
            {
                _equalizerFunc = value;
                _equalizer = Enumerable.Range(0, _bins * 2).Select(_equalizerFunc).ToArray();
            }
        }

        public bool UseWindow;

        public void AddSamples(WaveSamples samples)
        {
            _transformer.AddSamples(samples);
        }

        public event EventHandler<float[]> FrameAvailable;
    }
}