using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Utils;
using EasyAssertions;
using Xunit;

namespace BeatTracker.Test.Utils
{
    public class SignalProcessingTest
    {
        [Theory]
        [InlineData(new float[] {1, 2, 3, 4, 5, 6}, 2, 3, new float[] {1f, 2.5f, 4f, 5.5f})]
        [InlineData(new float[] {1, 2, 3, 4, 5, 6}, 1, 2, new float[] {1f, 3f, 5f})]
        public void Resample(IEnumerable<float> source, int p, int q, float[] expected)
        {
            var result = SignalProcessing.Resample(source, p, q).ToArray();

            result.Length.ShouldBe(expected.Count());
        }

        [Theory]
        [InlineData(15, 5, 3, 1)]
        [InlineData(2003, 80021, 2003, 80021)] // prime numbers
        [InlineData(44100, 22050, 2, 1)]
        public void TestRation(int a, int b, int expectedA, int expectedB)
        {
            var result = SignalProcessing.Ratio(a, b);
            result.a.ShouldBe(expectedA);
            result.b.ShouldBe(expectedB);
        }
    }
}