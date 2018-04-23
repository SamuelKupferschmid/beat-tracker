using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Tracking.Configuration;

namespace BeatTracker.Tracking.Parameters
{
    public abstract class GridSearch<T>
        where T : new()
    {
        protected GridSearch()
        {
        }

        public IEnumerable<(T configuration, double error)> Search(int iterations)
        {
            throw new NotImplementedException();
        }

        protected abstract double GetError(BeatAnalysisConfiguration configuration);
    }
}
