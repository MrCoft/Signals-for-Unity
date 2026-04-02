using System.Collections;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class ReactiveList<T> : IUntypedSignal, IList<T>
    {
        private readonly SignalContext _context;

        public int Timing;

        private readonly List<T> _committedValue = new();
        private readonly List<T> _pendingValue = new();
        private bool _isDirty;

        public int Level
        {
            get
            {
                return 0;
            }
        }
        public bool IsReady { get; set; } = true;
        public bool HasChangedThisPass { get; set; }
        public HashSet<IUntypedComputed> ComputedSubscribers { get; } = new();
        public HashSet<Effect> EffectSubscribers { get; } = new();

        public ReactiveList(SignalContext context, int timing)
        {
            _context = context;
            Timing = timing;
        }

        private void Track()
        {
            _context.DependenciesCollector.Add(this);
        }

        private void MarkDirty()
        {
            if (!_isDirty)
            {
                _isDirty = true;
                _context.TimingToDirtySignalsDict[Timing].Add(this);
            }
        }

        public void Update()
        {
            if (_isDirty)
            {
                foreach (var computed in ComputedSubscribers)
                {
                    _context.MarkComputedDirty(computed.Timing, computed);
                }

                foreach (var effect in EffectSubscribers)
                {
                    _context.TimingToDirtyEffectsDict[effect.Timing].Add(effect);
                }

                _committedValue.Clear();
                _committedValue.AddRange(_pendingValue);
            }

            HasChangedThisPass = _isDirty;
            _isDirty = false;
        }

        // NOTE: IList<T> Read operations — register dependency, read from committed

        public int Count
        {
            get
            {
                Track();
                return _committedValue.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public T this[int index]
        {
            get
            {
                Track();
                return _committedValue[index];
            }
            set
            {
                _pendingValue[index] = value;
                MarkDirty();
            }
        }

        // NOTE: Returns struct enumerator directly so foreach avoids boxing
        public List<T>.Enumerator GetEnumerator()
        {
            Track();
            return _committedValue.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            Track();
            return _committedValue.Contains(item);
        }

        public int IndexOf(T item)
        {
            Track();
            return _committedValue.IndexOf(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Track();
            _committedValue.CopyTo(array, arrayIndex);
        }

        // NOTE: IList<T> Write operations — mutate pending, mark dirty

        public void Add(T item)
        {
            _pendingValue.Add(item);
            MarkDirty();
        }

        public void Insert(int index, T item)
        {
            _pendingValue.Insert(index, item);
            MarkDirty();
        }

        public bool Remove(T item)
        {
            var removed = _pendingValue.Remove(item);
            if (removed)
            {
                MarkDirty();
            }

            return removed;
        }

        public void RemoveAt(int index)
        {
            _pendingValue.RemoveAt(index);
            MarkDirty();
        }

        public void Clear()
        {
            if (_pendingValue.Count > 0)
            {
                _pendingValue.Clear();
                MarkDirty();
            }
        }
    }
}
