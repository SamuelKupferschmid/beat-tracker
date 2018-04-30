using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Utils
{
    public class SpectrumLogger
    {
        public string Name { get; }

        private SpectrumLogger(string name)
        {
            Name = name;
        }

        public static SpectrumLogger Create(string name)
        {
            var logger = new SpectrumLogger(name);

            _instances.Add(logger);

            return logger;
        }

        public void AddSampe(float[] sample)
        {
            OnFrame?.Invoke(this, sample);
        }

        public void SetTitle(string title)
        {
            OnTitleChange?.Invoke(this, title);
        }

        public event EventHandler<float[]> OnFrame; 

        public event EventHandler<string> OnTitleChange; 

        private static readonly List<SpectrumLogger> _instances  = new List<SpectrumLogger>();

        public static IEnumerable<SpectrumLogger> Instances => _instances;
    }
}
