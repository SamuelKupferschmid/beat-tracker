using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Tracking.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    class ConfigurationRangeAttribute : Attribute
    {
        public ConfigurationRangeAttribute(int minValue, int maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Type = typeof(int);
        }

        public ConfigurationRangeAttribute(double minValue, double maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Type = typeof(double);
        }

        public object MinValue { get; }
        public object MaxValue { get; }

        public Type Type { get; }
    }
}
