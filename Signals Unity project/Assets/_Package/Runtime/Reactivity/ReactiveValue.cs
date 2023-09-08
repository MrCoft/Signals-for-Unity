namespace Coft.Signals
{
    public class ReactiveValue<T> : IReactiveValue<T>, IUntypedReactiveValue
    {
        private ReactivityManager _manager;

        public int Timing;

        private T _cachedValue;
        public bool IsDirty;
        private T _newValue;

        public ReactiveValue(ReactivityManager manager, int timing, T value)
        {
            _manager = manager;
            Timing = timing;
        }

        public T Value
        {
            get => _cachedValue;
            set
            {
                _newValue = value;
                IsDirty = true;
            }
        }
    }
}