using BeatTracker.Tracking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using EasyAssertions;

namespace BeatTracker.Test.Validation
{
    public class TempogramValidationTests
    {
        private const float MaxBpmDeviation = 5;
              
        private const string TempogramDataDirectory = @"C:\Users\Master AjaY\workspace\ip5bb\ballroom\TempogramData\";

        [Fact]
        public void ValidateSingle()
        {
            Error(Path.Combine(TempogramDataDirectory, "110-130bpm_click.wav"), 120).ShouldBeLessThan(MaxBpmDeviation);

            // Error(Path.Combine(TempogramDataDirectory, "Debussy_SonataViolinPianoGMinor-02_111_20080519-SMD-ss135-189.wav"), 300).ShouldBeLessThan(MaxBpmDeviation);
            // Error(Path.Combine(TempogramDataDirectory, "Faure_Op015-01_126_20100612-SMD-0-12.wav"), 120).ShouldBeLessThan(MaxBpmDeviation);
            // Error(Path.Combine(TempogramDataDirectory, "Poulenc_Valse_114_20100518-SMD-0-15.wav"), 120).ShouldBeLessThan(MaxBpmDeviation);
            // Error(Path.Combine(TempogramDataDirectory, "Schumann_Op015-03_113_20080115-SMD-0-13.wav"), 120).ShouldBeLessThan(MaxBpmDeviation);
        }

        private float Error(string fileName, float targetBpm)
        {
            var bpmList = new List<float>();

            var pulseAnalyzer = new PulseAnalyzer();
            pulseAnalyzer.PulseExtracted += (o, e) =>
            {
                var bpm = e.OrderByDescending(c => c.confidence).First().bpm;
                bpmList.Add(bpm);
            };

            var noveltyCurveExport = System.IO.File.ReadAllText($"{fileName}.noveltycurve");
            var noveltyCurveValues = noveltyCurveExport.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => float.Parse(x)).ToList();
            noveltyCurveValues.ForEach(x => pulseAnalyzer.AddFrame(x));

            return Math.Abs(bpmList.Average() - targetBpm);
        }
    }
}
