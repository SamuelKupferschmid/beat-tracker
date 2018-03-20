using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using BeatTracker.Tracking;

namespace BeatTracker.UI
{
    public class DumySpectrumProvider : ISpectrumProvider
    {
        private Random random = new Random();
        public DumySpectrumProvider()
        {
            Samples = new List<float[]>();
            var timer = new Timer(100);
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Samples.Add(Enumerable.Range(0,100).Select(i => (float)random.NextDouble()).ToArray());
            OnDataChanged();
        }

        public IList<float[]> Samples {get;}

        public string Name => "Dumy Data";

        public double SampleResolution { get; } = 0.01;

        public string SampleUnit { get; } = "seconds";

        public event EventHandler DataChanged;

        protected virtual void OnDataChanged()
        {
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}