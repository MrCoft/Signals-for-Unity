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

        public SignalObject(SignalContext context, int timing, T value, Action<T, T> copyFrom)
        {
            _signal = context.Signal(timing, value);
            _value1 = _signal.Peek();
            _value2 = new();
            _copyFrom = copyFrom;
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

        public T GetMutable()
        {
            var committed = _signal.Peek();
            var mutable = ReferenceEquals(committed, _value1) ? _value2 : _value1;
            _copyFrom(mutable, committed);
            _signal.Value = mutable;
            return mutable;
        }

        public void Dispose()
        {
            _signal.Dispose();
        }
    }
}
