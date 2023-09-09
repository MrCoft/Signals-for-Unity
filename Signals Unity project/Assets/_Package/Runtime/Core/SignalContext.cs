using System;
using System.Collections.Generic;
using System.Linq;

namespace Coft.Signals
{
    public class SignalContext
    {
        public HashSet<IUntypedSignal> DependenciesCollector = new();
        public bool IsDirty;
        
        private Dictionary<int, LinkedList<IUntypedSignal>> _timingToValuesDict = new();
        private Dictionary<int, LinkedList<IUntypedComputed>> _timingToComputedsDict = new();
        private Dictionary<int, LinkedList<Effect>> _timingToEffectsDict = new();

        public ISignal<T> Signal<T>(int timing, T value) where T : IEquatable<T>
        {
            var reactiveValue = new Signal<T>(this, timing, value);

            if (_timingToValuesDict.ContainsKey(timing) == false)
            {
                _timingToValuesDict.Add(timing, new());
            }

            _timingToValuesDict[timing].AddLast(reactiveValue);

            return reactiveValue;
        }

        public IReadOnlySignal<T> Computed<T>(int timing, Func<T> getter)
        {
            var computed = new Computed<T>(this, timing, getter);
            
            if (_timingToComputedsDict.ContainsKey(timing) == false)
            {
                _timingToComputedsDict.Add(timing, new());
            }
            
            _timingToComputedsDict[timing].AddLast(computed);
            
            return computed;
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
            
            // NOTE: update all computeds
            if (_timingToComputedsDict.ContainsKey(timing))
            {
                var computedsQueue = _timingToComputedsDict[timing]
                    .Where(signal => signal.Dependencies.Any(dep => dep.HasChangedThisPass))
                    .ToHashSet();
                
                foreach (var computed in computedsQueue)
                {
                    computed.IsReady = false;
                }
                
                var hasAnyRun = computedsQueue.Count > 0;
                while (hasAnyRun)
                {
                    hasAnyRun = false;

                    var newQueue = new HashSet<IUntypedComputed>();
                    
                    foreach (var computed in computedsQueue)
                    {
                        if (computed.Dependencies.All(dep => dep.IsReady))
                        {
                            computed.Run();
                            hasAnyRun = true;
                            foreach (var subscriber in computed.Subscribers.OfType<IUntypedComputed>())
                            {
                                if (subscriber.HasChangedThisPass)
                                {
                                    throw new Exception("Infinite loop detected");
                                }
                                
                                newQueue.Add(subscriber);
                            }
                        }
                        else
                        {
                            newQueue.Add(computed);
                        }
                    }

                    computedsQueue = newQueue;
                }

                if (computedsQueue.Count > 0)
                {
                    throw new Exception("Infinite loop detected");
                }
            }

            IsDirty = false;

            if (_timingToEffectsDict.ContainsKey(timing))
            {
                foreach (var effect in _timingToEffectsDict[timing])
                {
                    if (effect._currentDependencies.Any(signal => signal.HasChangedThisPass))
                    {
                        effect.Run();
                        // this should update dependencies
                    }
                }
            }

            if (IsDirty)
            {
                // repeat computeds and effects
            }
        }
    }
}
