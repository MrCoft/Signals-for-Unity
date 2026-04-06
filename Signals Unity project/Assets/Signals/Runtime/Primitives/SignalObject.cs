using System;

namespace Coft.Signals
{
    public class SignalObject<T> : ISignal<T>, IDisposable
        where T : class, new()
    {
        private readonly Signal<T> _signal;
        private readonly T _value1;
        private readonly T _value2;
        private readonly Action<T, T> _copyFrom;

        public SignalObject(SignalContext context, int timing, Action<T, T> copyFrom, T value)
        {
            _signal = context.Signal(timing, value);
            _value1 = _signal.Peek();
            _value2 = new();
            _copyFrom = copyFrom;
        }

        public void Dispose()
        {
            _signal.Dispose();
        }

        public T Value
        {
            get
            {
                return _signal.Value;
            }
        }

        public T Peek()
        {
            return _signal.Peek();
        }

        public T PeekLatest()
        {
            return _signal.PeekLatest();
        }

        public T GetMutableUninitialized()
        {
            var committed = _signal.Peek();
            var mutable = ReferenceEquals(committed, _value1) ? _value2 : _value1;
            _signal.Value = mutable;
            return mutable;
        }

        public T GetMutable()
        {
            var mutable = GetMutableUninitialized();
            _copyFrom(mutable, _signal.Peek());
            return mutable;
        }
    }
}
