using System;
using System.Collections.Generic;
using UniRx;

namespace Coft.Signals
{
    public class Effect
    {
        private readonly Action _action;
        private readonly HashSet<IObservable<Unit>> _allDependencies;
        private readonly HashSet<IObservable<Unit>> _currentDependencies;

        public Effect(Action action)
        {
            _action = action;
            _allDependencies = new HashSet<IObservable<Unit>>();
            _currentDependencies = new HashSet<IObservable<Unit>>();
            Run();
        }

        private void Run()
        {
            _currentDependencies.Clear();
            SignalContext.Observables.Clear();
            _action();
            foreach (var observable in SignalContext.Observables)
            {
                if (_allDependencies.Add(observable)) Register(observable);
                _currentDependencies.Add(observable);
            }
        }

        private void Register(IObservable<Unit> observable)
        {
            observable.Subscribe(_ =>
            {
                if (_currentDependencies.Contains(observable)) Run();
            });
        }
    }
}