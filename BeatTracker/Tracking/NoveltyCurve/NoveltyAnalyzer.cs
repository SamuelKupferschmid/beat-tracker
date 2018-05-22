using System;
using System.Collections.Generic;
using System.Linq;
using BeatTracker.Readers;
using BeatTracker.Utils;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;

namespace BeatTracker.Tracking.NoveltyCurve
{
    /// <summary>
    /// Extracts a curve from given stream which highlights beats in signals (Novelty Curve)
    /// </summary>
    public class NoveltyAnalyzer
    {
        private readonly IWaveStreamReader _streamReader;
        private readonly SpectrumLogger _inputLogger = SpectrumLogger.Create("FrequencyAnalyzer - Input");
        private readonly SpectrumLogger _outputLogger = SpectrumLogger.Create("FrequencyAnalyzer - Novelty Curve");

        
        private readonly BufferedTransformer _transformer;
        private readonly SpectrumAnalyzer _spectrumAnalyzer;

        public NoveltyParameters Parameters { get; private set; }

        private readonly SmoothedHannSubstraction _smoothedSubstraction;

        public NoveltyAnalyzer(IWaveStreamReader streamReader)
        {
            Parameters = new NoveltyParameters
            {
                WindowLength = 1024,
                StepSize = 512,
                DestinationSampleRate = 22050
            };

            _streamReader = streamReader;

            _spectrumAnalyzer = new SpectrumAnalyzer(Parameters);


            _smoothedSubstraction = new SmoothedHannSubstraction((int) Math.Max(Math.Ceiling(1.5 * Parameters.FeatureRate), 3));

            _transformer = new BufferedTransformer(Parameters.WindowLength);

            _transformer.FrameAvailable += _transformer_FrameAvailable;
            _streamReader.DataAvailable += _streamReader_DataAvailable;

            _transformer.AddSamples(new WaveSamples(new float[Parameters.WindowLength / 2], Parameters.WindowLength / 2)); // add 0-padding
        }

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

            data = SignalProcessing.Resample(data, Parameters.DestinationSampleRate, this._streamReader.WaveFormat.SampleRate)
                .ToArray();
            _transformer.AddSamples(new WaveSamples(data, data.Length));
        }

        private void _transformer_FrameAvailable(object sender, float[] e)
        {
            _inputLogger.AddSampe(e);

            var nextValue = _spectrumAnalyzer.Next(e);

            var noveltyCurve = _smoothedSubstraction.Next(nextValue);

            var log = new float[200];

            log[(int)noveltyCurve.Clamp(0, float.MaxValue)] = 1;
            _outputLogger.AddSampe(log);

            FrameAvailable?.Invoke(this, noveltyCurve);
        }

        
    }
}