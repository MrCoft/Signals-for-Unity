using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class SignalContext
    {
        public HashSet<IUntypedSignal> DependenciesCollector = new();
        public HashSet<IUntypedSignal> PreviousDependencies = new();
        public Dictionary<int, HashSet<IUntypedSignal>> TimingToDirtySignalsDict = new();
        public Dictionary<int, HashSet<Effect>> TimingToDirtyEffectsDict = new();

        private readonly Dictionary<int, List<HashSet<IUntypedComputed>>> _dirtyComputeds = new();
        private readonly List<IUntypedComputed> _levelBuffer = new();
        private readonly HashSet<IUntypedComputed> _committed = new();
        private readonly HashSet<Effect> _deferredEffects = new();
        private readonly List<string> _errors = new();

        private void InitializeTiming(int timing)
        {
            if (!TimingToDirtySignalsDict.ContainsKey(timing))
            {
                TimingToDirtySignalsDict.Add(timing, new());
                _dirtyComputeds.Add(timing, new());
                TimingToDirtyEffectsDict.Add(timing, new());
            }
        }

        public void MarkComputedDirty(int timing, IUntypedComputed computed)
        {
            computed.IsReady = false;
            var buckets = _dirtyComputeds[timing];
            var level = computed.Level;
            EnsureLevel(buckets, level);
            buckets[level].Add(computed);
        }

        public void RemoveDirtyComputed(int timing, IUntypedComputed computed)
        {
            var buckets = _dirtyComputeds[timing];
            var level = computed.Level;
            if (level < buckets.Count)
            {
                buckets[level].Remove(computed);
            }
        }

        public Signal<T> Signal<T>(int timing, T value, IEqualityComparer<T> comparer = null)
        {
            InitializeTiming(timing);
            return new(this, timing, value, comparer);
        }

        public Computed<T> Computed<T>(int timing, Func<T> getter, IEqualityComparer<T> comparer = null)
        {
            InitializeTiming(timing);
            return new(this, timing, getter, comparer);
        }

        public ReactiveList<T> List<T>(int timing)
        {
            InitializeTiming(timing);
            return new(this, timing);
        }

        public Effect Effect(int timing, Action action)
        {
            InitializeTiming(timing);
            return new(this, timing, action);
        }

        public void Update(int timing)
        {
            InitializeTiming(timing);

            _errors.Clear();
            var pass = 0;

            while (pass++ < 50)
            {
                FlushSignals(timing);
                FlushComputeds(timing, _errors);
                FlushEffects(timing, _errors);

                if (TimingToDirtySignalsDict[timing].Count == 0)
                {
                    break;
                }
            }

            if (TimingToDirtySignalsDict[timing].Count > 0)
            {
                _errors.Add("50 passes without update");
            }

            if (_errors.Count > 0)
            {
                throw new(string.Join("\n", _errors));
            }
        }

        private void FlushSignals(int timing)
        {
            foreach (var signal in TimingToDirtySignalsDict[timing])
            {
                signal.Update();
            }

            TimingToDirtySignalsDict[timing].Clear();
        }

        private void FlushComputeds(int timing, List<string> errors)
        {
            var buckets = _dirtyComputeds[timing];
            var maxLevel = Math.Max(8, CountDirty(buckets) * 2);
            var cycleErrorAdded = false;
            _committed.Clear();

            for (var levelIndex = 0; levelIndex < buckets.Count; levelIndex++)
            {
                var bucket = buckets[levelIndex];
                if (bucket.Count == 0)
                {
                    continue;
                }

                _levelBuffer.Clear();
                foreach (var c in bucket)
                {
                    _levelBuffer.Add(c);
                }

                bucket.Clear();

                foreach (var computed in _levelBuffer)
                {
                    if (_committed.Contains(computed))
                    {
                        continue;
                    }

                    if (!TryRun(computed, errors))
                    {
                        computed.Update();
                        continue;
                    }

                    var newLevel = computed.Level;
                    if (newLevel > maxLevel)
                    {
                        if (!cycleErrorAdded)
                        {
                            errors.Add("Could not resolve signal graph; possible cycle detected; undefined behavior will follow");
                            cycleErrorAdded = true;
                        }
                        CommitComputed(computed, buckets, timing);
                        _committed.Add(computed);
                    }
                    else if (HasUncommittedDep(computed))
                    {
                        EnsureLevel(buckets, newLevel);
                        buckets[newLevel].Add(computed);
                    }
                    else
                    {
                        CommitComputed(computed, buckets, timing);
                    }
                }
            }

            foreach (var bucket in buckets)
            {
                bucket.Clear();
            }
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

                try
                {
                    effect.Run();
                }
                catch (Exception e)
                {
                    errors.Add(e.ToString());
                }
            }

            TimingToDirtyEffectsDict[timing].Clear();
            TimingToDirtyEffectsDict[timing].UnionWith(_deferredEffects);
        }

        private void CommitComputed(IUntypedComputed computed, List<HashSet<IUntypedComputed>> buckets, int timing)
        {
            computed.Update();
            if (!computed.HasChangedThisPass)
            {
                return;
            }

            foreach (var subscriber in computed.ComputedSubscribers)
            {
                if (_committed.Contains(subscriber))
                {
                    continue;
                }

                var level = subscriber.Level;
                EnsureLevel(buckets, level);
                buckets[level].Add(subscriber);
            }

            TimingToDirtyEffectsDict[timing].UnionWith(computed.EffectSubscribers);
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

        private static void EnsureLevel(List<HashSet<IUntypedComputed>> buckets, int level)
        {
            while (buckets.Count <= level)
            {
                buckets.Add(new());
            }
        }

        private static int CountDirty(List<HashSet<IUntypedComputed>> buckets)
        {
            var count = 0;

            foreach (var bucket in buckets)
            {
                count += bucket.Count;
            }

            return count;
        }

        private static bool HasUncommittedDep(IUntypedComputed computed)
        {
            foreach (var dep in computed.Dependencies)
            {
                if (!dep.IsReady)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasDirtySignalDep(Effect effect, HashSet<IUntypedSignal> dirtySignals)
        {
            foreach (var dep in effect.Dependencies)
            {
                if (dirtySignals.Contains(dep))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
