namespace BeatTracker.Tracking.Configuration
{
    public class BeatAnalysisConfiguration
    {
        [ConfigurationRange(1,1000)]
        public int FrequenzyAnalyzerWindowSize { get; set; }

        [ConfigurationRange(1,10)]
        public int FrequenzyAnalyzerSmoothSize { get; set; }
    }
}
