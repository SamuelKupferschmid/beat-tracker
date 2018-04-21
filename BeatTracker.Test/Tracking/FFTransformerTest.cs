using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using BeatTracker.Tracking;
using EasyAssertions;
using MathNet.Numerics.Distributions;
using NAudio.Dsp;
using NAudio.Wave;
using Xunit;

namespace BeatTracker.Test.Tracking
{
    public class FFTransformerTest
    {
        [Fact]
        public void FrameAvailableTriggeredWhenEnoughData()
        {
            var testee = new FFTransformer(100);

            int count = 0;

            testee.FrameAvailable += (sender, floats) => count++;

            var data = Enumerable.Repeat(1f, 40).ToArray();
            var sample = new WaveSamples(data, data.Length);

            testee.AddSamples(sample);
            testee.AddSamples(sample);
            count.ShouldBe(0);
            testee.AddSamples(sample);
            count.ShouldBe(1);
        }


        [Fact]
        public void FindSineWaves()
        {
            var sampleRate = 44100;
            var freq0 = 440;
            var freq1 = 4000;

            var data = new float[sampleRate * 10];

            for (var i = 0; i < data.Length; i++)
            {
                var r = i * (1f / sampleRate);
                data[i] = (float) (Math.Sin(r * freq0) + Math.Sin(r * freq1));
            }

            var transformer = new FFTransformer(10000);
            float[] result = null;
            transformer.FrameAvailable += (sender, floats) => result = floats;
            transformer.AddSamples(new WaveSamples(data, data.Length));

            result.ShouldNotBeNull();
            Math.Abs(result[freq0]).ShouldBeGreaterThan(0f);
            Math.Abs(result[freq1]).ShouldBeGreaterThan(0f);
        }

        [Fact]
        public void ZeroEqualizerCancelOutAllSignal()
        {
            var data = Normal.Samples(0, 1).Take(100).Select(v => (float)v).ToArray();

            var result = new List<float>();
            var transformer = new FFTransformer(40);

            transformer.EqualizerFunc = i => 0;

            transformer.FrameAvailable += (sender, floats) => result.AddRange(floats);
            transformer.AddSamples(new WaveSamples(data, data.Length));

            result.AllItemsSatisfy(v => v.ShouldBe(0f));

        }
    }
}