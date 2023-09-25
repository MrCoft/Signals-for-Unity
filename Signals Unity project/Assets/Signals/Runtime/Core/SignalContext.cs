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

        public Computed<T> Computed<T>(int timing, Func<T> getter) where T : IEquatable<T>
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
            var errors = new List<string>();

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
                    var computedsQueue = new HashSet<IUntypedComputed>();
                    var newQueue = new HashSet<IUntypedComputed>();

                    foreach (var computed in TimingToDirtyComputedsDict[timing])
                    {
                        // NOTE: gives us dependency lists
                        var runWorked = true;
                        try
                        {
                            computed.Run();
                        }
                        catch (Exception e)
                        {
                            errors.Add(e.ToString());
                            runWorked = false;
                        }

                        if (runWorked)
                        {
                            computed.IsReady = false;
                            newQueue.Add(computed);
                        }
                        else
                        {
                            computed.Update();
                        }
                    }

                    foreach (var computed in newQueue)
                    {
                        if (computed.Dependencies.All(dep => dep.IsReady))
                        {
                            computed.Update();
                        }
                        else
                        {
                            computedsQueue.Add(computed);
                        }
                    }

                    while (computedsQueue.Count > 0)
                    {
                        var hasAnyRun = false;

                        newQueue = new HashSet<IUntypedComputed>();

                        foreach (var computed in computedsQueue)
                        {
                            if (computed.Dependencies.All(dep => dep.IsReady))
                            {
                                var runWorked = true;
                                try
                                {
                                    computed.Run();
                                }
                                catch (Exception e)
                                {
                                    errors.Add(e.ToString());
                                    runWorked = false;
                                }
                                
                                hasAnyRun = true;

                                if (runWorked == false)
                                {
                                    computed.Update();
                                    continue;
                                }
                                
                                if (computed.Dependencies.Any(dep => dep.IsReady == false))
                                {
                                    computed.IsReady = false;
                                    newQueue.Add(computed);
                                }
                                else
                                {
                                    computed.Update();

                                    if (computed.HasChangedThisPass)
                                    {
                                        foreach (var subscriber in computed.ComputedSubscribers)
                                        {
                                            if (subscriber.HasChangedThisPass == false)
                                            {
                                                // throw new Exception("Infinite loop detected");
                                                newQueue.Add(subscriber);
                                            }
                                        }
                                        
                                        TimingToDirtyEffectsDict[timing].UnionWith(computed.EffectSubscribers);
                                    }
                                }
                            }
                            else
                            {
                                newQueue.Add(computed);
                            }
                        }

                        if (hasAnyRun == false)
                        {
                            errors.Add("Could not resolve signal graph; possible cycle detected; undefined behavior will follow");

                            var computed = computedsQueue.Aggregate((curMin, x) =>
                            {
                                if (curMin == null)
                                {
                                    return x;
                                }

                                var curMinDeps = curMin.Dependencies.Count(dep => dep.IsReady == false);
                                var xDeps = x.Dependencies.Count(dep => dep.IsReady == false);

                                if (curMinDeps < xDeps)
                                {
                                    return curMin;
                                }

                                if (curMinDeps == xDeps)
                                {
                                    var curMinResolvedDeps = curMin.Dependencies.Count(dep => dep.IsReady);
                                    var xResolvedDeps = x.Dependencies.Count(dep => dep.IsReady);
                                    return curMinResolvedDeps >= xResolvedDeps ? curMin : x;
                                }

                                return x;
                            });

                            var runWorked = true;
                            try
                            {
                                computed.Run();
                            }
                            catch (Exception e)
                            {
                                errors.Add(e.ToString());
                                runWorked = false;
                            }
                            
                            computed.Update();
                            if (runWorked && computed.HasChangedThisPass)
                            {
                                foreach (var subscriber in computed.ComputedSubscribers)
                                {
                                    if (subscriber.HasChangedThisPass == false)
                                    {
                                        // throw new Exception("Infinite loop detected");
                                        newQueue.Add(subscriber);
                                    }
                                }
                                    
                                TimingToDirtyEffectsDict[timing].UnionWith(computed.EffectSubscribers);
                            }
                        }

                        computedsQueue = newQueue;
                    }
                    
                    TimingToDirtyComputedsDict[timing].Clear();
                }

                {
                    var nextQueue = new HashSet<Effect>();
                    
                    foreach (var effect in TimingToDirtyEffectsDict[timing])
                    {
                        if (effect.Dependencies.Any(dep => TimingToDirtySignalsDict[timing].Contains(dep)))
                        {
                            nextQueue.Add(effect);
                            continue;
                        }
                        
                        var runWorked = true;
                        try
                        {
                            effect.Run();
                        }
                        catch (Exception e)
                        {
                            errors.Add(e.ToString());
                            runWorked = false;
                        }
                    }
                    
                    TimingToDirtyEffectsDict[timing].Clear();
                    TimingToDirtyEffectsDict[timing].UnionWith(nextQueue);
                }

                if (TimingToDirtySignalsDict[timing].Count > 0)
                {
                    needsAnotherPass = true;
                }
            }

            if (needsAnotherPass)
            {
                errors.Add("50 passes without update");
            }

            if (errors.Count > 0)
            {
                throw new Exception(string.Join("\n", errors));
            }
        }
    }
}
