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
            if (!TimingToDirtySignalsDict.ContainsKey(timing))
            {
                TimingToDirtySignalsDict.Add(timing, new());
                TimingToDirtyComputedsDict.Add(timing, new());
                TimingToDirtyEffectsDict.Add(timing, new());
            }
        }

        public Signal<T> Signal<T>(int timing, T value) where T : IEquatable<T>
        {
            InitializeTiming(timing);
            return new(this, timing, value);
        }

        public Computed<T> Computed<T>(int timing, Func<T> getter) where T : IEquatable<T>
        {
            InitializeTiming(timing);
            return new(this, timing, getter);
        }

        public Effect Effect(int timing, Action action)
        {
            InitializeTiming(timing);
            return new(this, timing, action);
        }

        public void Update(int timing)
        {
            InitializeTiming(timing);

            var errors = new List<string>();
            var pass = 0;

            while (pass++ < 50)
            {
                FlushSignals(timing);
                FlushComputeds(timing, errors);
                FlushEffects(timing, errors);

                if (TimingToDirtySignalsDict[timing].Count == 0) break;
            }

            if (TimingToDirtySignalsDict[timing].Count > 0)
                errors.Add("50 passes without update");

            if (errors.Count > 0)
                throw new(string.Join("\n", errors));
        }

        private void FlushSignals(int timing)
        {
            foreach (var signal in TimingToDirtySignalsDict[timing])
                signal.Update();
            TimingToDirtySignalsDict[timing].Clear();
        }

        private void FlushComputeds(int timing, List<string> errors)
        {
            // Run all dirty computeds to discover their current dependencies.
            // Mark them not-ready so downstream nodes know they haven't committed yet.
            var pending = new HashSet<IUntypedComputed>();
            foreach (var computed in TimingToDirtyComputedsDict[timing])
            {
                if (TryRun(computed, errors))
                {
                    computed.IsReady = false;
                    pending.Add(computed);
                }
                else
                {
                    computed.Update();
                }
            }

            // Commit those whose deps are all settled. Defer the rest.
            var deferred = new HashSet<IUntypedComputed>();
            foreach (var computed in pending)
            {
                if (!computed.Dependencies.All(dep => dep.IsReady) || HasStaleComputedDep(computed))
                    deferred.Add(computed);
                else
                    CommitComputed(computed, deferred, timing);
            }

            // Resolve deferred in dependency order, breaking cycles if stuck.
            while (deferred.Count > 0)
            {
                var next = new HashSet<IUntypedComputed>();
                var anyRan = false;

                foreach (var computed in deferred)
                {
                    if (!computed.Dependencies.All(dep => dep.IsReady))
                    {
                        next.Add(computed);
                        continue;
                    }

                    anyRan = true;

                    if (!TryRun(computed, errors))
                    {
                        computed.Update();
                        continue;
                    }

                    if (computed.Dependencies.Any(dep => !dep.IsReady))
                    {
                        computed.IsReady = false;
                        next.Add(computed);
                    }
                    else
                    {
                        CommitComputed(computed, next, timing);
                    }
                }

                if (!anyRan)
                    BreakCycle(deferred, next, timing, errors);

                deferred = next;
            }

            TimingToDirtyComputedsDict[timing].Clear();
        }

        private void FlushEffects(int timing, List<string> errors)
        {
            var deferred = new HashSet<Effect>();

            foreach (var effect in TimingToDirtyEffectsDict[timing])
            {
                if (effect.Dependencies.Any(dep => TimingToDirtySignalsDict[timing].Contains(dep)))
                {
                    deferred.Add(effect);
                    continue;
                }

                try { effect.Run(); }
                catch (Exception e) { errors.Add(e.ToString()); }
            }

            TimingToDirtyEffectsDict[timing].Clear();
            TimingToDirtyEffectsDict[timing].UnionWith(deferred);
        }

        private void CommitComputed(IUntypedComputed computed, HashSet<IUntypedComputed> queue, int timing)
        {
            computed.Update();
            if (!computed.HasChangedThisPass) return;

            queue.UnionWith(computed.ComputedSubscribers);
            TimingToDirtyEffectsDict[timing].UnionWith(computed.EffectSubscribers);
        }

        private static bool HasStaleComputedDep(IUntypedComputed computed)
        {
            foreach (var dep in computed.Dependencies)
            {
                if (dep is IUntypedComputed && dep.HasChangedThisPass)
                    return true;
            }
            return false;
        }

        private bool TryRun(IUntypedComputed computed, List<string> errors)
        {
            try
            {
                computed.Run();
                return true;
            }
            catch (Exception e)
            {
                errors.Add(e.ToString());
                return false;
            }
        }

        private void BreakCycle(HashSet<IUntypedComputed> deferred, HashSet<IUntypedComputed> next, int timing, List<string> errors)
        {
            errors.Add("Could not resolve signal graph; possible cycle detected; undefined behavior will follow");

            var computed = ComputedWithFewestUnreadyDeps(deferred);
            if (TryRun(computed, errors))
                CommitComputed(computed, next, timing);
            else
                computed.Update();
        }

        private static IUntypedComputed ComputedWithFewestUnreadyDeps(HashSet<IUntypedComputed> computeds)
        {
            IUntypedComputed best = null;
            var bestUnready = int.MaxValue;
            var bestReady = -1;

            foreach (var computed in computeds)
            {
                var unready = 0;
                var ready = 0;
                foreach (var dep in computed.Dependencies)
                {
                    if (dep.IsReady) ready++;
                    else unready++;
                }

                if (unready < bestUnready || unready == bestUnready && ready > bestReady)
                {
                    best = computed;
                    bestUnready = unready;
                    bestReady = ready;
                }
            }

            return best;
        }
    }
}
