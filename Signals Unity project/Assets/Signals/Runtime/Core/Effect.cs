using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class Effect : IDisposable
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
        
        public void Dispose()
        {
            _context.TimingToDirtyEffectsDict[Timing].Remove(this);
            foreach (var signal in Dependencies)
            {
                signal.EffectSubscribers.Remove(this);
            }
        }

        ~Effect()
        {
            Dispose();
        }

        public void Run()
        {
            foreach (var signal in Dependencies)
            {
                signal.EffectSubscribers.Remove(this);
            }
            
            var dependenciesCopy = new HashSet<IUntypedSignal>(Dependencies);
            Dependencies.Clear();
            _context.DependenciesCollector.Clear();
            try
            {
                _action();
            }
            catch (Exception e)
            {
                Dependencies.UnionWith(dependenciesCopy);
                foreach (var signal in dependenciesCopy)
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
