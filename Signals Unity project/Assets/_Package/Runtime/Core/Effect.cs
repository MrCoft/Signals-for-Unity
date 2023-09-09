using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class Effect
    {
        private SignalContext _context;

        public int Timing;
        
        private readonly Action _action;
        public HashSet<IUntypedSignal> Dependencies;

        public Effect(SignalContext context, int timing, Action action)
        {
            _context = context;
            Timing = timing;
            _action = action;
            Dependencies = new HashSet<IUntypedSignal>();
            _context.TimingToDirtyEffectsDict[timing].Add(this);
        }

        public void Run()
        {
            Dependencies.Clear();
            _context.DependenciesCollector.Clear();
            _action();
            foreach (var signal in _context.DependenciesCollector)
            {
                Dependencies.Add(signal);
                signal.EffectSubscribers.Add(this);
            }
        }
    }
}
