namespace Coft.Signals
{
    public class ComputedSignal<T> : IReadOnlySignal<T>, IUntypedSignal
    {
        private SignalManager _manager;

        public int Timing;

        private T _cachedValue;

        public ComputedSignal(SignalManager manager, int timing, T value)
        {
            _manager = manager;
            Timing = timing;
            _cachedValue = value;
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
    }
}
