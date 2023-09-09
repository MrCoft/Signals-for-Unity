using System;
using System.Collections.Generic;
using System.Linq;

namespace Coft.Signals
{
    public class Computed<T> : IUntypedComputed, IDisposable
    {
        private SignalContext _context;

        public int Timing;

        private readonly Func<T> _getter;
        private T _cachedValue;
        
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
            Dependencies = new();
            ComputedSubscribers = new();
            EffectSubscribers = new();
            IsReady = false;
            HasChangedThisPass = false;
            HasRun = false;
            _context.TimingToDirtyComputedsDict[timing].Add(this);
        }

        public void Dispose()
        {
            _context.TimingToDirtyComputedsDict[Timing].Remove(this);
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
        }

        public void Run()
        {
            Dependencies.Clear();
            // _currentDependencies.Clear();
            _context.DependenciesCollector.Clear();
            // _action();
            var newValue = _getter();
            HasChangedThisPass = _cachedValue.Equals(newValue) == false;
            _cachedValue = newValue;
            foreach (var signal in _context.DependenciesCollector)
            {
                // if (_allDependencies.Add(signal)) Register(signal);
                // _currentDependencies.Add(signal);
                Dependencies.Add(signal);
                signal.ComputedSubscribers.Add(this);
            }

            IsReady = true;
        }
    }
}
