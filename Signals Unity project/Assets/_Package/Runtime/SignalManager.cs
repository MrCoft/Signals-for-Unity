using System;
using System.Collections.Generic;
using System.Linq;

namespace Coft.Signals
{
    public class SignalManager
    {
        public HashSet<IUntypedSignal> DependenciesCollector = new();
        public bool IsDirty;
        
        private Dictionary<int, LinkedList<IUntypedSignal>> _timingToValuesDict = new();
        private Dictionary<int, LinkedList<Effect>> _timingToEffectsDict = new();

        public ISignal<T> CreateSignal<T>(int timing, T value)
        {
            var reactiveValue = new Signal<T>(this, timing, value);

            if (_timingToValuesDict.ContainsKey(timing) == false)
            {
                _timingToValuesDict.Add(timing, new());
            }

            _timingToValuesDict[timing].AddLast(reactiveValue);

            return reactiveValue;
        }

        public IReadOnlySignal<T> Computed<T>(Func<T> getter)
        {
            return null;
        }

        public void Effect(int timing, Action action)
        {
            var effect = new Effect(this, timing, action);
            
            if (_timingToEffectsDict.ContainsKey(timing) == false)
            {
                _timingToEffectsDict.Add(timing, new());
            }
            
            _timingToEffectsDict[timing].AddLast(effect);
        }

        public void Update(int timing)
        {
            if (_timingToValuesDict.ContainsKey(timing) == false)
            {
                return;
            }

            foreach (var reactiveValue in _timingToValuesDict[timing])
            {
                reactiveValue.Update();
            }
            
            // TODO LATER
            // after all computeds are computed

            IsDirty = false;
            
            foreach (var effect in _timingToEffectsDict[timing])
            {
                if (effect._currentDependencies.OfType<Signal<int>>().Any(signal => signal.HasChangedThisPass))
                {
                    effect.Run();
                    // this should update dependencies
                }
            }

            if (IsDirty)
            {
                // repeat computeds and effects
            }
        }
    }
}
