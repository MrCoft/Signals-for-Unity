using System;
using System.Collections.Generic;
using System.Linq;

namespace Coft.Signals
{
    public class ComputedSignal<T> : IUntypedSignal, IUntypedComputed, IReadOnlySignal<T>
    {
        private SignalManager _manager;

        public int Timing;

        private readonly Func<T> _getter;
        private T _cachedValue;
        
        public bool HasChangedThisPass { get; set; }

        public HashSet<IUntypedSignal> Dependencies { get; }
        public bool IsReady { get; set; }
        public HashSet<IUntypedSignal> Subscribers { get; }
        
        public ComputedSignal(SignalManager manager, int timing, Func<T> getter)
        {
            _manager = manager;
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
                _manager.DependenciesCollector.Add(this);
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
            _manager.DependenciesCollector.Clear();
            // _action();
            var newValue = _getter();
            HasChangedThisPass = _cachedValue.Equals(newValue) == false;
            _cachedValue = newValue;
            foreach (var signal in _manager.DependenciesCollector)
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
