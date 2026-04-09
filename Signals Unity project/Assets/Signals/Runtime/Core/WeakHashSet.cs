using System;
using System.Collections;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class WeakHashSet<T> : IEnumerable<T> where T : class
    {
        private readonly List<WeakReference<T>> _entries = new();

        public void Add(T item)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].TryGetTarget(out var target) && ReferenceEquals(target, item))
                {
                    return;
                }
            }

            _entries.Add(new(item));
        }

        public void Remove(T item)
        {
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                if (!_entries[i].TryGetTarget(out var target) || ReferenceEquals(target, item))
                {
                    _entries[i] = _entries[_entries.Count - 1];
                    _entries.RemoveAt(_entries.Count - 1);
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new(_entries);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly List<WeakReference<T>> _entries;
            private int _index;

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
            public T Current { get; private set; }

            public Enumerator(List<WeakReference<T>> entries)
            {
                _entries = entries;
                _index = -1;
                Current = default;
            }

            public bool MoveNext()
            {
                while (++_index < _entries.Count)
                {
                    if (_entries[_index].TryGetTarget(out var target))
                    {
                        Current = target;
                        return true;
                    }

                    _entries[_index] = _entries[_entries.Count - 1];
                    _entries.RemoveAt(_entries.Count - 1);
                    _index--;
                }

                return false;
            }

            public void Reset()
            {
                _index = -1;
            }
            public void Dispose() { }
        }
    }
}
