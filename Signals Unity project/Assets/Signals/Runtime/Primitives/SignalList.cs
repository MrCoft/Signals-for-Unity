using System;
using System.Collections;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class SignalList<T> : IUntypedSignal, ISignalList<T>, IDisposable
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

        public SignalList(SignalContext context, int timing)
        {
            _context = context;
            Timing = timing;
        }

        public List<T> GetMutable()
        {
            if (_isDirty)
            {
                _pendingValue.Clear();
                _pendingValue.AddRange(_committedValue);
            }
            else
            {
                _isDirty = true;
                _context.TimingToDirtySignalsDict[Timing].Add(this);
            }

            return _pendingValue;
        }

        public List<T> Peek()
        {
            return _committedValue;
        }

        public List<T> PeekLatest()
        {
            return _pendingValue;
        }

        // NOTE: Reactive reads — IReadOnlyList<T> from committed, with tracking

        public int Count
        {
            get
            {
                _context.DependenciesCollector.Add(this);
                return _committedValue.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                _context.DependenciesCollector.Add(this);
                return _committedValue[index];
            }
        }

        public List<T>.Enumerator GetEnumerator()
        {
            _context.DependenciesCollector.Add(this);
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

        public void Dispose()
        {
            _context.TimingToDirtySignalsDict[Timing].Remove(this);
        }
    }
}
