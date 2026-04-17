using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class ListChangeTracker<T>
    {
        private readonly Func<IReadOnlyList<T>> _getter;

        public ListChangeTracker(Func<IReadOnlyList<T>> getter)
        {
            _getter = getter;
        }

        public List<T> Added = new();
        public List<T> Removed = new();
        public IReadOnlyList<T> List;

        private HashSet<T> _lastSnapshot = new();
        private HashSet<T> _currentSnapshot = new();

        public void Update()
        {
            Added.Clear();
            Removed.Clear();
            _currentSnapshot.Clear();

            List = _getter() ?? Array.Empty<T>();

            for (var i = 0; i < List.Count; i++)
            {
                var item = List[i];
                _currentSnapshot.Add(item);

                if (!_lastSnapshot.Contains(item))
                {
                    Added.Add(item);
                }
            }

            foreach (var item in _lastSnapshot)
            {
                if (!_currentSnapshot.Contains(item))
                {
                    Removed.Add(item);
                }
            }

            (_lastSnapshot, _currentSnapshot) = (_currentSnapshot, _lastSnapshot);
        }
    }
}
