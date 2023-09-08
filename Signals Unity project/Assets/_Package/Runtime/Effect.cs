using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class Effect
    {
        private SignalManager _manager;

        public int Timing;
        
        private readonly Action _action;
        private readonly HashSet<IUntypedSignal> _allDependencies;
        public HashSet<IUntypedSignal> _currentDependencies;

        public Effect(SignalManager manager, int timing, Action action)
        {
            _manager = manager;
            Timing = timing;
            _action = action;
            _allDependencies = new HashSet<IUntypedSignal>();
            _currentDependencies = new HashSet<IUntypedSignal>();
            Run();
        }

        public void Run()
        {
            _currentDependencies.Clear();
            _manager.DependenciesCollector.Clear();
            _action();
            foreach (var signal in _manager.DependenciesCollector)
            {
                if (_allDependencies.Add(signal)) Register(signal);
                _currentDependencies.Add(signal);
            }
        }

        private void Register(IUntypedSignal observable)
        {
            // observable.Subscribe(_ =>
            // {
            //     if (_currentDependencies.Contains(observable)) Run();
            // });
        }
    }
}
