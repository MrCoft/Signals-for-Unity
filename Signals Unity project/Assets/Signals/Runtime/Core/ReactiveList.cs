using System.Collections;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class ReactiveList<T> : IUntypedSignal, IList<T>
    {
        private readonly SignalContext _context;
        private readonly List<T> _items = new();

        public int Timing;
        public bool IsDirty;

        public int Level
        {
            get
            {
                return 0;
            }
        }
        public bool IsReady { get; set; }
        public bool HasChangedThisPass { get; set; }
        public HashSet<IUntypedComputed> ComputedSubscribers { get; } = new();
        public HashSet<Effect> EffectSubscribers { get; } = new();

        public ReactiveList(SignalContext context, int timing)
        {
            _context = context;
            Timing = timing;
            IsReady = true;
        }

        private void Track()
        {
            _context.DependenciesCollector.Add(this);
        }

        private void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
                _context.TimingToDirtySignalsDict[Timing].Add(this);
            }
        }

        public void Update()
        {
            if (IsDirty)
            {
                foreach (var computed in ComputedSubscribers)
                {
                    _context.MarkComputedDirty(Timing, computed);
                }

                _context.TimingToDirtyEffectsDict[Timing].UnionWith(EffectSubscribers);
            }
            HasChangedThisPass = IsDirty;
            IsDirty = false;
        }

        // Read operations — register dependency

        public int Count
        {
            get
            {
                Track();
                return _items.Count;
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
                return _items[index];
            }
            set
            {
                _items[index] = value;
                MarkDirty();
            }
        }

        // Returns struct enumerator directly so foreach avoids boxing
        public List<T>.Enumerator GetEnumerator()
        {
            Track();
            return _items.GetEnumerator();
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
            return _items.Contains(item);
        }
        public int IndexOf(T item)
        {
            Track();
            return _items.IndexOf(item);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            Track();
            _items.CopyTo(array, arrayIndex);
        }

        // Write operations — mark dirty

        public void Add(T item)
        {
            _items.Add(item);
            MarkDirty();
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            MarkDirty();
        }

        public bool Remove(T item)
        {

            var removed = _items.Remove(item);
            if (removed) MarkDirty();
            return removed;
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            MarkDirty();
        }

        public void Clear()
        {
            if (_items.Count > 0)
            {
                _items.Clear();
                MarkDirty();
            }
        }
    }
}
