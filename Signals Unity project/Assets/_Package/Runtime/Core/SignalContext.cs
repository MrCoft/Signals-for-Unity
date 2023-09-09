using System;
using System.Collections.Generic;
using System.Linq;

namespace Coft.Signals
{
    public class SignalContext
    {
        public HashSet<IUntypedSignal> DependenciesCollector = new();
        public Dictionary<int, HashSet<IUntypedSignal>> TimingToDirtySignalsDict = new();
        public Dictionary<int, HashSet<IUntypedComputed>> TimingToDirtyComputedsDict = new();
        public Dictionary<int, HashSet<Effect>> TimingToDirtyEffectsDict = new();

        private void InitializeTiming(int timing)
        {
            if (TimingToDirtySignalsDict.ContainsKey(timing) == false)
            {
                TimingToDirtySignalsDict.Add(timing, new());
                TimingToDirtyComputedsDict.Add(timing, new());
                TimingToDirtyEffectsDict.Add(timing, new());
            }
        }
        
        public Signal<T> Signal<T>(int timing, T value) where T : IEquatable<T>
        {
            InitializeTiming(timing);
            
            var signal = new Signal<T>(this, timing, value);
            return signal;
        }

        public Computed<T> Computed<T>(int timing, Func<T> getter)
        {
            InitializeTiming(timing);
            
            var computed = new Computed<T>(this, timing, getter);
            return computed;
        }
        
        public Effect Effect(int timing, Action action)
        {
            InitializeTiming(timing);
            
            var effect = new Effect(this, timing, action);
            return effect;
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

                {
                    foreach (var signal in TimingToDirtySignalsDict[timing])
                    {
                        signal.Update();
                    }

                    TimingToDirtySignalsDict[timing].Clear();
                }

                // NOTE: update all computeds
                {
                    var computedsQueue = TimingToDirtyComputedsDict[timing];

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
                                foreach (var subscriber in computed.ComputedSubscribers)
                                {
                                    if (subscriber.HasChangedThisPass)
                                    {
                                        throw new Exception("Infinite loop detected");
                                    }

                                    newQueue.Add(subscriber);
                                }

                                if (computed.HasChangedThisPass)
                                    // ALSO CHECK ELSWHERE?
                                {
                                    TimingToDirtyEffectsDict[timing].UnionWith(computed.EffectSubscribers);
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
                    
                    TimingToDirtyComputedsDict[timing].Clear();
                }

                {
                    foreach (var effect in TimingToDirtyEffectsDict[timing])
                    {
                        effect.Run();
                    }
                    
                    TimingToDirtyEffectsDict[timing].Clear();
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
