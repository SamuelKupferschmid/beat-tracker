using BeatTracker.Tracking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using EasyAssertions;
using Xunit.Abstractions;

namespace BeatTracker.Test.Validation
{
    public class BallroomValidationTests
    {
        private const float MaxBpmDeviation = 5;

        private const string BpmAnnotationDirectory = @"C:\Users\Master AjaY\workspace\ip5bb\ballroom\BallroomAnnotations\ballroomGroundTruth\";

        private readonly ITestOutputHelper _output;

        public BallroomValidationTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void ValidateSingle()
        {
            //var fileName = @"C:\Users\Master AjaY\workspace\ip5bb\ballroom\BallroomData\ChaChaCha\Albums-Cafe_Paradiso-05.wav";
            //(float bpmTarget, float bpmDeviation, float bpmDeviationInPercent) = Error(fileName);
            //bpmDeviation.ShouldBeLessThan(MaxBpmDeviation);
        }

        [Fact]
        public void ValidateGenre()
        {
            var baseDirectory = @"C:\Users\Master AjaY\workspace\ip5bb\ballroom\BallroomData\";

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("genre;name;bpm_target;bpm_predicted");

            foreach (var genreDirectory in Directory.EnumerateDirectories(baseDirectory))
            {
                //var bpmDeviations = new List<float>();
                //var bpmDeviationsInPercent = new List<float>();

                // var genreDirectory = @"C:\Users\Master AjaY\workspace\ip5bb\ballroom\BallroomData\Waltz\";

                foreach (var filename in Directory.EnumerateFiles(genreDirectory))
                {
                    if (filename.EndsWith(".wav"))
                    {
                        (float bpmTarget, float bpmPredicted) = Error(filename);
                        csvBuilder.AppendLine($"{Path.GetFileName(genreDirectory)};{Path.GetFileName(filename)};{bpmTarget};{bpmPredicted}");

                        //bpmDeviations.Add(bpmDeviation);
                        //bpmDeviationsInPercent.Add(bpmDeviationInPercent);
                    }
                }

                //if (bpmDeviations.Any())
                //{
                //    //File.WriteAllText(Path.Combine(genreDirectory, "Eval.txt"), csvBuilder.ToString());

                //    //File.WriteAllText(Path.Combine(genreDirectory, "BpmDeviations.txt"), string.Join(Environment.NewLine, bpmDeviations.Select(e => $"{e:F}").ToArray()));
                //    //File.WriteAllText(Path.Combine(genreDirectory, "BpmDeviationsInPercent.txt"), string.Join(Environment.NewLine, bpmDeviationsInPercent.Select(e => $"{e * 100:F}").ToArray()));

                //    // errorList.Average().ShouldBeLessThan(MaxBpmDeviation);

                //    _output.WriteLine($"{Path.GetFileName(genreDirectory)}: {bpmDeviations.Average():F} average bpm deviation. In percent: {bpmDeviationsInPercent.Average() * 100:F}%.");
                //}
            }

            File.WriteAllText(Path.Combine(baseDirectory, "Evaluation_BallroomDataset.csv"), csvBuilder.ToString());
        }

        private (float bpmTarget, float bpmPredicted) Error(string fileName)
        {
            var bpmTarget = float.Parse(File.ReadAllText(Path.Combine(BpmAnnotationDirectory, $"{Path.GetFileNameWithoutExtension(fileName)}.bpm")));

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

            //var bpmDeviation = Math.Abs(bpmList.Average() - bpmTarget);
            //var bpmDeviationInPercent = bpmDeviation / bpmTarget;

            var bpmPredicted = bpmList.Average();

            return (bpmTarget, bpmPredicted);
        }
    }
}
