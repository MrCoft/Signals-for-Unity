using System.Collections.Generic;

namespace Coft.Signals
{
    public class Signal<T> : IUntypedSignal, ISignal<T>
    {
        private readonly SignalContext _context;
        private readonly IEqualityComparer<T> _comparer;

        public int Timing;

        private T _committedValue;
        private T _pendingValue;
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

        public Signal(SignalContext context, int timing, T value = default, IEqualityComparer<T> comparer = null)
        {
            _context = context;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            Timing = timing;
            _committedValue = value;
            _pendingValue = value;
        }

        public T Peek() => _committedValue;

        public T PeekLatest() => _pendingValue;

        public T Value
        {
            get
            {
                _context.DependenciesCollector.Add(this);
                return _committedValue;
            }
            set
            {
                if (!_comparer.Equals(value, _pendingValue))
                {
                    _pendingValue = value;
                    _isDirty = true;
                    _context.TimingToDirtySignalsDict[Timing].Add(this);
                }
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
            }

            HasChangedThisPass = _isDirty;
            _committedValue = _pendingValue;
            _isDirty = false;
        }
    }
}
