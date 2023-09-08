using System;
using UniRx;

namespace Coft.Signals
{
    public class Signal<T>
    {
        private readonly ReactiveProperty<T> _property;
        private readonly IObservable<Unit> _unitObservable;

        public Signal(T initialValue = default)
        {
            _property = new ReactiveProperty<T>(initialValue);
            _unitObservable = _property.Select(_ => Unit.Default);
        }

        public Signal(ReactiveProperty<T> property)
        {
            _property = property;
            _unitObservable = _property.Select(_ => Unit.Default);
        }

        public T Value
        {
            get
            {
                SignalContext.Observables.Add(_unitObservable);
                return _property.Value;
            }

            set => _property.Value = value;
        }
    }
}