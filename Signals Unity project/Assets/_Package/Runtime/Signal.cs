namespace Coft.Signals
{
    public class Signal<T> : ISignal<T>, IUntypedSignal
    {
        private SignalManager _manager;

        public int Timing;

        private T _cachedValue;
        public bool IsDirty;
        public bool HasChangedThisPass;
        private T _newValue;

        public Signal(SignalManager manager, int timing, T value)
        {
            _manager = manager;
            Timing = timing;
            
            _cachedValue = value;
            _newValue = value;
            IsDirty = false;
        }

        public T Value
        {
            get
            {
                _manager.DependenciesCollector.Add(this);
                return _cachedValue;
            }
            set
            {
                _newValue = value;
                IsDirty = true;
            }
        }

        public void Update()
        {
            HasChangedThisPass = IsDirty;
            _cachedValue = _newValue;
            IsDirty = false;
        }
    }
}