using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class SignalContext
    {
        public HashSet<IUntypedSignal> DependenciesCollector = new();
        public HashSet<IUntypedSignal> PreviousDependencies = new();
        public Dictionary<int, HashSet<IUntypedSignal>> TimingToDirtySignalsDict = new();
        public Dictionary<int, HashSet<IUntypedComputed>> TimingToDirtyComputedsDict = new();
        public Dictionary<int, HashSet<Effect>> TimingToDirtyEffectsDict = new();

        // Pre-allocated working buffers — reused every Update() call, never re-created.
        private readonly List<IUntypedComputed> _pending = new();
        private readonly HashSet<IUntypedComputed> _committed = new();
        private readonly HashSet<IUntypedComputed> _deferredA = new();
        private readonly HashSet<IUntypedComputed> _deferredB = new();
        private readonly HashSet<Effect> _deferredEffects = new();

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
            _pending.Clear();
            _committed.Clear();

            // Run all dirty computeds to discover their current dependencies.
            // Mark them not-ready so downstream nodes know they haven't committed yet.
            foreach (var computed in TimingToDirtyComputedsDict[timing])
            {
                if (TryRun(computed, errors))
                {
                    computed.IsReady = false;
                    _pending.Add(computed);
                }
                else
                {
                    computed.Update();
                }
            }

            // Commit those whose deps are all settled. Defer the rest.
            var deferred = _deferredA;
            deferred.Clear();
            foreach (var computed in _pending)
            {
                if (!AllDepsReady(computed) || HasStaleComputedDep(computed))
                    deferred.Add(computed);
                else
                    CommitComputed(computed, deferred, timing);
            }

            // Resolve deferred in dependency order, breaking cycles if stuck.
            while (deferred.Count > 0)
            {
                var next = ReferenceEquals(deferred, _deferredA) ? _deferredB : _deferredA;
                next.Clear();
                var anyRan = false;

                foreach (var computed in deferred)
                {
                    if (!AllDepsReady(computed))
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

                    if (HasUnreadyDep(computed))
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
            _deferredEffects.Clear();
            var dirtySignals = TimingToDirtySignalsDict[timing];

            foreach (var effect in TimingToDirtyEffectsDict[timing])
            {
                if (HasDirtySignalDep(effect, dirtySignals))
                {
                    _deferredEffects.Add(effect);
                    continue;
                }

                try { effect.Run(); }
                catch (Exception e) { errors.Add(e.ToString()); }
            }

            TimingToDirtyEffectsDict[timing].Clear();
            TimingToDirtyEffectsDict[timing].UnionWith(_deferredEffects);
        }

        private void CommitComputed(IUntypedComputed computed, HashSet<IUntypedComputed> queue, int timing)
        {
            computed.Update();
            _committed.Add(computed);
            if (!computed.HasChangedThisPass) return;

            foreach (var subscriber in computed.ComputedSubscribers)
            {
                if (!_committed.Contains(subscriber))
                    queue.Add(subscriber);
            }
            TimingToDirtyEffectsDict[timing].UnionWith(computed.EffectSubscribers);
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

        private static bool AllDepsReady(IUntypedComputed computed)
        {
            foreach (var dep in computed.Dependencies)
                if (!dep.IsReady) return false;
            return true;
        }

        private static bool HasUnreadyDep(IUntypedComputed computed)
        {
            foreach (var dep in computed.Dependencies)
                if (!dep.IsReady) return true;
            return false;
        }

        private static bool HasStaleComputedDep(IUntypedComputed computed)
        {
            foreach (var dep in computed.Dependencies)
                if (dep is IUntypedComputed && dep.HasChangedThisPass)
                    return true;
            return false;
        }

        private static bool HasDirtySignalDep(Effect effect, HashSet<IUntypedSignal> dirtySignals)
        {
            foreach (var dep in effect.Dependencies)
                if (dirtySignals.Contains(dep)) return true;
            return false;
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
