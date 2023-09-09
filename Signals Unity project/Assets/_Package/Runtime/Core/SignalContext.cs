using System;
using System.Collections.Generic;
using System.Linq;

namespace Coft.Signals
{
    public class SignalContext
    {
        public HashSet<IUntypedSignal> DependenciesCollector = new();
        public Dictionary<int, HashSet<IUntypedSignal>> TimingToDirtySignalsDict = new();
        
        private Dictionary<int, HashSet<IUntypedSignal>> _timingToSignalsDict = new();
        private Dictionary<int, HashSet<IUntypedComputed>> _timingToComputedsDict = new();
        private Dictionary<int, HashSet<Effect>> _timingToEffectsDict = new();

        private void InitializeTiming(int timing)
        {
            if (_timingToSignalsDict.ContainsKey(timing) == false)
            {
                _timingToSignalsDict.Add(timing, new());
                _timingToComputedsDict.Add(timing, new());
                _timingToEffectsDict.Add(timing, new());
                TimingToDirtySignalsDict.Add(timing, new());
            }
        }
        
        public Signal<T> Signal<T>(int timing, T value) where T : IEquatable<T>
        {
            InitializeTiming(timing);
            
            var signal = new Signal<T>(this, timing, value);
            _timingToSignalsDict[timing].Add(signal);
            return signal;
        }

        public Computed<T> Computed<T>(int timing, Func<T> getter)
        {
            InitializeTiming(timing);
            
            var computed = new Computed<T>(this, timing, getter);
            _timingToComputedsDict[timing].Add(computed);
            return computed;
        }
        
        public void Effect(int timing, Action action)
        {
            InitializeTiming(timing);
            
            var effect = new Effect(this, timing, action);
            _timingToEffectsDict[timing].Add(effect);
        }

        public void Update(int timing)
        {
            InitializeTiming(timing);
            
            var needsAnotherPass = true;
            var passNumber = 0;

            while (needsAnotherPass && passNumber < 50)
            {
                needsAnotherPass = false;
                passNumber += 1;
                
                foreach (var signal in TimingToDirtySignalsDict[timing])
                {
                    signal.Update();
                }

                TimingToDirtySignalsDict[timing].Clear();

                // NOTE: update all computeds
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
                
                foreach (var effect in _timingToEffectsDict[timing])
                {
                    if (effect.HasRun == false || effect.Dependencies.Any(signal => signal.HasChangedThisPass))
                    {
                        effect.Run();
                    }
                }

                if (TimingToDirtySignalsDict[timing].Count > 0)
                {
                    needsAnotherPass = true;
                }
            }

            if (needsAnotherPass)
            {
                throw new Exception("50 passes without update");
            }
        }
    }
}
