using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Tracking.Configuration;

namespace BeatTracker.Tracking.Parameters
{
    public class ParameterRangeDefinition<T>
    {

        public static IEnumerable<ParameterRangeDefinition<T>> CreateDefinitions<T>()
        where T : new()
        {
            foreach (var property in typeof(T).GetProperties())
            {
                foreach (var customAttributeData in property.CustomAttributes.Where(c => c.AttributeType == typeof(ConfigurationRangeAttribute)))
                {
                    yield return new ParameterRangeDefinition<T>();
                }
            }
        }
    }
}
