using System.Collections.Generic;

namespace Coft.Signals
{
    public class Signal<T> : IUntypedSignal
    {
        private SignalContext _context;

        public int Timing;

        private T _cachedValue;
        public int Level => 0;
        public bool IsDirty;
        public bool HasChangedThisPass { get; set; }
        public bool IsReady { get; set; }
        private T _newValue;
        private readonly IEqualityComparer<T> _comparer;
        public HashSet<IUntypedComputed> ComputedSubscribers { get; }
        public HashSet<Effect> EffectSubscribers { get; }

        public Signal(SignalContext context, int timing, T value, IEqualityComparer<T> comparer = null)
        {
            _context = context;
            Timing = timing;
            _comparer = comparer ?? EqualityComparer<T>.Default;

            _cachedValue = value;
            _newValue = value;
            IsDirty = false;
            IsReady = true;
            ComputedSubscribers = new();
            EffectSubscribers = new();
        }

        public T Value
        {
            get
            {
                _context.DependenciesCollector.Add(this);
                return _cachedValue;
            }
            set
            {
                if (_comparer.Equals(value, _newValue) == false)
                {
                    _newValue = value;
                    IsDirty = true;
                    _context.TimingToDirtySignalsDict[Timing].Add(this);
                }
            }
        }

        public void Update()
        {
            if (IsDirty)
            {
                foreach (var computed in ComputedSubscribers)
                    _context.MarkComputedDirty(Timing, computed);
                _context.TimingToDirtyEffectsDict[Timing].UnionWith(EffectSubscribers);
            }

            HasChangedThisPass = IsDirty;
            _cachedValue = _newValue;
            IsDirty = false;
        }
    }
}
