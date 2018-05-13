using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Readers;
using BeatTracker.Tracking;
using EasyAssertions;
using FakeItEasy;
using Xunit;

namespace BeatTracker.Test.Tracking
{
    public class FrequencyAnalyzerTest
    {
        private FrequencyAnalyzer _testee;
        private MonoWaveFileReader _waveFileReader;

        public FrequencyAnalyzerTest()
        {
            _waveFileReader = new MonoWaveFileReader("data\\ag1.wav",false);
            _testee = new FrequencyAnalyzer(_waveFileReader);

        }
        [Fact]
        public void CompareNoveltyCurveWithTempogramToolbox()
        {
            var data = File.ReadAllText("data\\ag1_noveltyCurveTemplate.txt");
            var expectedCurve = data.Split('\t').Select(Convert.ToSingle).ToList();

            var result = new List<float>();

            _testee.FrameAvailable += (sender, f) => result.Add(f);

            _waveFileReader.Start();


            result.Count.ShouldBe(expectedCurve.Count);

            var error = result.Zip(expectedCurve, (a, b) => Math.Pow(a - b, 2)).Average();

            error.ShouldBeLessThan(100d);



        }
    }
}
