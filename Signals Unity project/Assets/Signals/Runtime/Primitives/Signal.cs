using System.Collections.Generic;

namespace Coft.Signals
{
    public class Signal<T> : IUntypedSignal
    {
        private readonly SignalContext _context;
        private readonly IEqualityComparer<T> _comparer;

        public int Timing;

        private T _cachedValue;
        private T _newValue;
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
            _cachedValue = value;
            _newValue = value;
        }

        public T Peek() => _cachedValue;

        public T PeekLatest() => _newValue;

        public T Value
        {
            get
            {
                _context.DependenciesCollector.Add(this);
                return _cachedValue;
            }
            set
            {
                if (!_comparer.Equals(value, _newValue))
                {
                    _newValue = value;
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
            _cachedValue = _newValue;
            _isDirty = false;
        }
    }
}
