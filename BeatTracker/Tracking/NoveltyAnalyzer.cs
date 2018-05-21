using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using BeatTracker.Utils;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using NAudio.Utils;
using NAudio.Wave;

namespace BeatTracker.Tracking
{
    /// <summary>
    /// Extracts a curve from given stream which highlights beats in signals (Novelty Curve)
    /// </summary>
    public class NoveltyAnalyzer
    {
        private readonly IWaveStreamReader _streamReader;
        private readonly SpectrumLogger _inputLogger = SpectrumLogger.Create("FrequencyAnalyzer - Input");
        private readonly SpectrumLogger _outputLogger = SpectrumLogger.Create("FrequencyAnalyzer - Novelty Curve");

        private readonly float[] StftWindow;
        private readonly int WindowLength;
        private readonly int StepSize;
        private readonly double compressionC = 1000;
        private readonly bool logCompression;
        private readonly double resampleFeatureRate = 200;
        private readonly int destSampleRate = 22050;
        private BufferedTransformer _transformer;

        private int _coefficientRangeEnd;

        public NoveltyAnalyzer(IWaveStreamReader streamReader)
        {
            int fftWindowSize = 1024;

            _streamReader = streamReader;
            this.StftWindow = MathNet.Numerics.Window.Hann(fftWindowSize).Select(Convert.ToSingle).ToArray();
            this.WindowLength = 1024;
            this.StepSize = 512;
            this._coefficientRangeEnd = (int) (WindowLength / 2) + 1; //floor values
            this.FeatureRate = destSampleRate / StepSize;

            _transformer = new BufferedTransformer(WindowLength);

            _transformer.FrameAvailable += _transformer_FrameAvailable;
            _streamReader.DataAvailable += _streamReader_DataAvailable;

            _transformer.AddSamples(new WaveSamples(new float[WindowLength / 2], WindowLength / 2)); // add 0-padding
        }

        public int FeatureRate { get; private set; }

        public event EventHandler<float> FrameAvailable;

        private void _streamReader_DataAvailable(object sender, WaveSamples e)
        {
            float[] data;
            if (e.Data.Length > e.Length)
            {
                data = new float[e.Length];
                Array.Copy(e.Data, data, e.Length);
            }
            else
            {
                data = e.Data;
            }

            data = SignalProcessing.Resample(data, this.destSampleRate, this._streamReader.WaveFormat.SampleRate)
                .ToArray();
            _transformer.AddSamples(new WaveSamples(data, data.Length));
        }

        private void _transformer_FrameAvailable(object sender, float[] e)
        {
            _inputLogger.AddSampe(e);

            var spec = StftSpectrogram(e);

            //normalize and convert to dB scale
            var max = spec.Max();
            var threshold = (float)Math.Pow(10, -74d / 20);

            for (int i = 0; i < spec.Length; i++)
            {
                spec[i] = Math.Max((float)(spec[i] / max), threshold);
            }

            var noveltyCurve = e.Select(Math.Abs).Sum();

            var log = new float[200];

            log[(int)noveltyCurve.Clamp(0, float.MaxValue)] = 1;
            _outputLogger.AddSampe(log);

            FrameAvailable?.Invoke(this, noveltyCurve);
        }

        private float[] StftSpectrogram(float[] data)
        {
            var ffTransoformed = new Complex32[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                ffTransoformed[i] = data[i] * StftWindow[i];
            }
            MathNet.Numerics.IntegralTransforms.Fourier.Forward(ffTransoformed);

            return ffTransoformed.Select(c => c.Magnitude).Take(_coefficientRangeEnd).ToArray();
        }
    }
}