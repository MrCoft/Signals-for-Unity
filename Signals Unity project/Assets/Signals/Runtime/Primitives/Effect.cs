using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class Effect : IDisposable
    {
        private readonly SignalContext _context;
        private readonly Action _action;

        public int Timing;

        public HashSet<IUntypedSignal> Dependencies = new();

        public Effect(SignalContext context, int timing, Action action)
        {
            _context = context;
            _action = action;
            Timing = timing;

            _context.TimingToDirtyEffectsDict[timing].Add(this);
        }

        public void Dispose()
        {
            _context.TimingToDirtyEffectsDict[Timing].Remove(this);

            foreach (var signal in Dependencies)
            {
                signal.EffectSubscribers.Remove(this);
            }
        }

        public void Run()
        {
            foreach (var signal in Dependencies)
            {
                signal.EffectSubscribers.Remove(this);
            }

            var previousDeps = _context.PreviousDependencies;
            previousDeps.Clear();
            previousDeps.UnionWith(Dependencies);
            Dependencies.Clear();
            _context.DependenciesCollector.Clear();

            try
            {
                _action();
            }
            catch (Exception e)
            {
                Dependencies.UnionWith(previousDeps);

                foreach (var signal in previousDeps)
                {
                    signal.EffectSubscribers.Add(this);
                }

                throw e;
            }

            foreach (var signal in _context.DependenciesCollector)
            {
                Dependencies.Add(signal);
                signal.EffectSubscribers.Add(this);
            }
        }
    }
}
