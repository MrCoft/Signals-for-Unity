using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class Effect : IDisposable
    {
        private readonly SignalContext _context;
        private readonly Func<IDisposable> _action;

        private IDisposable _cleanup;

        public int Timing;

        public HashSet<IUntypedSignal> Dependencies = new();

        public Effect(SignalContext context, int timing, Func<IDisposable> action)
        {
            _context = context;
            _action = action;
            Timing = timing;

            _context.TimingToDirtyEffectsDict[timing].Add(this);
        }

        public void Dispose()
        {
            _cleanup?.Dispose();
            _cleanup = null;

            _context.TimingToDirtyEffectsDict[Timing].Remove(this);

            foreach (var signal in Dependencies)
            {
                signal.EffectSubscribers.Remove(this);
            }
        }

        public void Run()
        {
            _cleanup?.Dispose();
            _cleanup = null;

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
                _cleanup = _action();
            }
            catch
            {
                Dependencies.UnionWith(previousDeps);

                foreach (var signal in previousDeps)
                {
                    signal.EffectSubscribers.Add(this);
                }

                throw;
            }

            foreach (var signal in _context.DependenciesCollector)
            {
                Dependencies.Add(signal);
                signal.EffectSubscribers.Add(this);
            }
        }
    }
}
