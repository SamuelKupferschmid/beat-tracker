using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using BeatTracker.Utils;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.Statistics;
using NAudio.Utils;
using NAudio.Wave;

namespace BeatTracker.Tracking
{
    /// <summary>
    /// Extracts a curve from given stream which highlights beats in signals (Novelty Curve)
    /// </summary>
    public class FrequencyAnalyzer
    {
        private readonly IWaveStreamReader _streamReader;
        private readonly FFTransformer _transformer;
        private readonly SpectrumLogger _inputLogger = SpectrumLogger.Create("FrequencyAnalyzer - Input");
        private readonly SpectrumLogger _outputLogger = SpectrumLogger.Create("FrequencyAnalyzer - Novelty Curve");

        public FrequencyAnalyzer(IWaveStreamReader streamReader)
        {
            _streamReader = streamReader;

            int fftWindowSize = 1024;
            int fftStepSize = 512;

            _transformer = new FFTransformer(fftWindowSize, fftStepSize, 0, 512);
            _transformer.UseWindow = true;

            _transformer.FrameAvailable += _transformer_FrameAvailable;
            _streamReader.DataAvailable += _streamReader_DataAvailable;
        }

        public event EventHandler<float> FrameAvailable;

        private void _streamReader_DataAvailable(object sender, WaveSamples e)
        {
            _transformer.AddSamples(e);
        }

        private readonly Queue<double> buffer = new Queue<double>(8);

        private float compression = 1000;

        private int counter = 0;
        private float smoothedValue = 0;
        private float smoothWindow = 50;

        private void _transformer_FrameAvailable(object sender, float[] e)
        {
            _inputLogger.AddSampe(e);

            /*
                % normalize and convert to dB
                specData = specData./max(max(specData));
                thresh = -74; % dB
                thresh = 10^(thresh./20);
                specData = (max(specData,thresh));
             */

            //var thresh = -74f; // dB
            //thresh = (float)Math.Pow(10, thresh / 20);

            //var max = e.Max();
            //var x = new float[e.Length];
            //var y = new float[e.Length];
            //for (int i = 0; i < e.Length; i++)
            //{
            //    x[i] = i;
            //    y[i] = (float)(Math.Log(1 + Math.Max(e[i] / max, thresh) * compression) / Math.Log(1 + compression));
            //}


            //_inputLogger.AddSampe(y);

            var noveltyCurve = e.Select(Math.Abs).Sum();

            ++counter;

            //smoothedValue += noveltyCurve / smoothWindow;

            //if (counter > smoothWindow)
            //{
            //    smoothedValue -= smoothedValue / smoothWindow;
            //}

            //noveltyCurve -= smoothedValue;

            var log = new float[300];

            log[(int)noveltyCurve.Clamp(0, float.MaxValue)] = 1;
            _outputLogger.AddSampe(log);

            FrameAvailable?.Invoke(this, noveltyCurve);
        }
    }
}