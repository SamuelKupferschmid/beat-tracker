using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Tracking
{
    public class SmoothedHannSubstraction
    {
        private readonly int _length;
        private readonly float[] _window;
        private Queue<float> _noveltyQueue;

        public SmoothedHannSubstraction(int length)
        {
            _length = length;
            _window = MathNet.Numerics.Window.Hann(_length).Select(Convert.ToSingle).ToArray();
            _noveltyQueue = new Queue<float>(Enumerable.Repeat(0f,_length));
        }

        public float Next(float value)
        {
            _noveltyQueue.Dequeue();
            _noveltyQueue.Enqueue(value);

            return value;
        }
    }
}
