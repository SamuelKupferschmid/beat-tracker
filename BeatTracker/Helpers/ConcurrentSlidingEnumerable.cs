using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatTracker.Helpers
{
    public class ConcurrentSlidingEnumerable<T> : IEnumerable<T>
    {
        private readonly int _maxCount;
        private readonly T[] _items;

        private int _currentIndex;

        private readonly object _syncRoot = new object();

        public ConcurrentSlidingEnumerable(int maxCount)
        {
            if (maxCount < 1)
                throw new ArgumentException($"{nameof(maxCount)} must be greater than zero");

            _maxCount = maxCount;
            _items = new T[_maxCount];
        }

        public void Push(T item)
        {
            lock (_syncRoot)
            {
                ++_currentIndex;
                if (_currentIndex == _maxCount)
                    _currentIndex = 0;

                _items[_currentIndex] = item;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock(_syncRoot)
            {
                return ((IEnumerable<T>)_items).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock(_syncRoot)
            {
                return _items.GetEnumerator();
            }
        }
    }
}
