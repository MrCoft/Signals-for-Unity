using System;
using System.Collections.Generic;
using System.Linq;

namespace Coft.Signals
{
    public class Computed<T> : IUntypedSignal, IUntypedComputed
    {
        private SignalContext _context;

        public int Timing;

        private readonly Func<T> _getter;
        private T _cachedValue;
        
        public bool HasChangedThisPass { get; set; }

        public HashSet<IUntypedSignal> Dependencies { get; }
        public bool IsReady { get; set; }
        public HashSet<IUntypedSignal> Subscribers { get; }
        
        public Computed(SignalContext context, int timing, Func<T> getter)
        {
            _context = context;
            Timing = timing;
            _getter = getter;
            Dependencies = new();
            Subscribers = new();
            IsReady = false;
            HasChangedThisPass = false;

            // Run();
            // HasChangedThisPass = false;
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
                signal.Subscribers.Add(this);
            }

            IsReady = true;
        }
    }
}
