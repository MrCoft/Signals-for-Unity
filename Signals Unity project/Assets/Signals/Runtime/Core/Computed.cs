using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class Computed<T> : IUntypedComputed, IDisposable where T : IEquatable<T>
    {
        private SignalContext _context;

        public int Timing;
        public int Level { get; private set; }

        private readonly Func<T> _getter;
        private T _cachedValue;
        private T _newValue;
        
        public bool HasChangedThisPass { get; set; }

        public HashSet<IUntypedSignal> Dependencies { get; }
        public bool IsReady { get; set; }
        public HashSet<IUntypedComputed> ComputedSubscribers { get; }
        public HashSet<Effect> EffectSubscribers { get; }
        public bool HasRun { get; set; }
        
        public Computed(SignalContext context, int timing, Func<T> getter)
        {
            _context = context;
            Timing = timing;
            _getter = getter;
            _cachedValue = default;
            _newValue = default;
            Dependencies = new();
            ComputedSubscribers = new();
            EffectSubscribers = new();
            IsReady = false;
            HasChangedThisPass = false;
            HasRun = false;
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
            HasChangedThisPass = _newValue.Equals(_cachedValue) == false;
            if (_newValue.Equals(_cachedValue) == false)
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
                    signal.ComputedSubscribers.Add(this);
                throw e;
            }
            Dependencies.UnionWith(_context.DependenciesCollector);
            foreach (var signal in _context.DependenciesCollector)
                signal.ComputedSubscribers.Add(this);

            var maxDepLevel = 0;
            foreach (var dep in _context.DependenciesCollector)
                if (dep.Level > maxDepLevel) maxDepLevel = dep.Level;
            Level = maxDepLevel + 1;
        }
    }
}
