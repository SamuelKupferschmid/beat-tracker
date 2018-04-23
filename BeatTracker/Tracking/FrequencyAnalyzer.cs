﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using BeatTracker.Tracking.Configuration;
using BeatTracker.Utils;
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
        private readonly BeatAnalysisConfiguration _configuration;
        private readonly IWaveStreamReader _streamReader;
        private readonly FFTransformer _transformer;
        private readonly SpectrumLogger _inputLogger = SpectrumLogger.Create("FrequencyAnalyzer - Input");
        private readonly SpectrumLogger _outputLogger = SpectrumLogger.Create("FrequencyAnalyzer - Novelty Curve");

        public FrequencyAnalyzer(BeatAnalysisConfiguration configuration, IWaveStreamReader streamReader)
        {
            _configuration = configuration;
            _streamReader = streamReader;
            _transformer = new FFTransformer(configuration.FrequenzyAnalyzerWindowSize);
            _transformer.FrameAvailable += _transformer_FrameAvailable;
            _streamReader.DataAvailable += _streamReader_DataAvailable;
            _rawFile = File.OpenWrite("C:\\tmp\\novelty.dat");
        }

        public event EventHandler<float> FrameAvailable;

        private void _streamReader_DataAvailable(object sender, WaveSamples e)
        {
            _transformer.AddSamples(e);
        }

        private readonly Queue<double> buffer = new Queue<double>(8);
        private FileStream _rawFile;

        private int counter = 0;
        private float smoothedValue = 0;
        private float smoothWindow = 50;

        private void _transformer_FrameAvailable(object sender, float[] e)
        {
            _inputLogger.AddSampe(e);

            var noveltyCurve = e.Select(Math.Abs).Sum();

            counter++;
            smoothedValue += noveltyCurve / smoothWindow;

            if (counter > smoothWindow)
            {
                smoothedValue -= smoothedValue / smoothWindow;
            }

            noveltyCurve -= smoothedValue;

            var log = new float[100];

            log[(int)noveltyCurve.Clamp(0, float.MaxValue)] = 1;
            _outputLogger.AddSampe(log);

            FrameAvailable?.Invoke(this, noveltyCurve);
        }
    }
}