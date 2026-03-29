using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class Computed<T> : IUntypedComputed, IDisposable
    {
        private readonly SignalContext _context;
        private readonly Func<T> _getter;
        private readonly IEqualityComparer<T> _comparer;

        public int Timing { get; }

        private T _cachedValue;
        private T _newValue;

        public int Level { get; private set; }
        public bool IsReady { get; set; }
        public bool HasChangedThisPass { get; set; }
        public HashSet<IUntypedSignal> Dependencies { get; } = new();
        public HashSet<IUntypedComputed> ComputedSubscribers { get; } = new();
        public HashSet<Effect> EffectSubscribers { get; } = new();

        public Computed(SignalContext context, int timing, Func<T> getter, IEqualityComparer<T> comparer = null)
        {
            _context = context;
            _getter = getter;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            Timing = timing;

            _context.MarkComputedDirty(timing, this);
        }

        public void Dispose()
        {
            _context.RemoveDirtyComputed(Timing, this);

            foreach (var signal in Dependencies)
            {
                signal.ComputedSubscribers.Remove(this);
            }
        }

        ~Computed()
        {
            Dispose();
        }

        public T Peek() => _cachedValue;

        public T Value
        {
            get
            {
                _context.DependenciesCollector.Add(this);
                return _cachedValue;
            }
        }

        public void Update()
        {
            HasChangedThisPass = !_comparer.Equals(_newValue, _cachedValue);

            if (HasChangedThisPass)
            {
                _cachedValue = _newValue;
            }

            IsReady = true;
        }

        public void Run()
        {
            foreach (var signal in Dependencies)
            {
                signal.ComputedSubscribers.Remove(this);
            }

            var previousDeps = _context.PreviousDependencies;
            previousDeps.Clear();
            previousDeps.UnionWith(Dependencies);
            Dependencies.Clear();
            _context.DependenciesCollector.Clear();

            try
            {
                _newValue = _getter();
            }
            catch (Exception e)
            {
                Dependencies.UnionWith(previousDeps);

                foreach (var signal in previousDeps)
                {
                    signal.ComputedSubscribers.Add(this);
                }

                throw e;
            }

            Dependencies.UnionWith(_context.DependenciesCollector);

            foreach (var signal in _context.DependenciesCollector)
            {
                signal.ComputedSubscribers.Add(this);
            }

            var maxDepLevel = 0;

            foreach (var dep in _context.DependenciesCollector)
            {
                if (dep.Level > maxDepLevel)
                {
                    maxDepLevel = dep.Level;
                }
            }

            Level = maxDepLevel + 1;
        }
    }
}
