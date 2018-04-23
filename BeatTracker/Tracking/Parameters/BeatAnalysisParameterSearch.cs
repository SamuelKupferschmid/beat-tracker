using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Tracking.Configuration;

namespace BeatTracker.Tracking.Parameters
{
    public class BeatAnalysisParameterSearch : GridSearch<BeatAnalysisConfiguration>
    {
        public BeatAnalysisParameterSearch()
        {
        }

        protected override double GetError(BeatAnalysisConfiguration configuration)
        {
            throw new NotImplementedException();
        }
    }
}
