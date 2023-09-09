using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class Signal<T> : ISignal<T>, IUntypedSignal where T : IEquatable<T>
    {
        private SignalContext _context;

        public int Timing;

        private T _cachedValue;
        public bool IsDirty;
        public bool HasChangedThisPass { get; set; }
        public bool IsReady { get; set; }
        private T _newValue;
        public HashSet<IUntypedSignal> Subscribers { get; }

        public Signal(SignalContext context, int timing, T value)
        {
            _context = context;
            Timing = timing;
            
            _cachedValue = value;
            _newValue = value;
            IsDirty = false;
            IsReady = true;
            Subscribers = new();
        }

        public T Value
        {
            get
            {
                _context.DependenciesCollector.Add(this);
                return _cachedValue;
            }
            set
            {
                if (value.Equals(_newValue) == false)
                {
                    _newValue = value;
                    IsDirty = true;
                }
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
