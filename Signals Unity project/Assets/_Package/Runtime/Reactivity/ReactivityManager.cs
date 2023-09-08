using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class ReactivityManager
    {
        private Dictionary<int, LinkedList<IUntypedReactiveValue>> _timingToValuesDict;

        public IReactiveValue<T> CreateValue<T>(int timing, T value)
        {
            var reactiveValue = new ReactiveValue<T>(this, timing, value);

            if (_timingToValuesDict.ContainsKey(timing) == false)
            {
                _timingToValuesDict.Add(timing, new());
            }

            _timingToValuesDict[timing].AddLast(reactiveValue);

            return reactiveValue;
        }

        public IReadOnlyReactiveValue<T> Computed<T>(Func<T> getter)
        {
            return null;
        }

        public void Effect(Action action)
        {

        }
    }
}