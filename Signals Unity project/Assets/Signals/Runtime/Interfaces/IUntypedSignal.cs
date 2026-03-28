using System.Collections.Generic;

namespace Coft.Signals
{
    public interface IUntypedSignal
    {
        int Level { get; }
        bool IsReady { get; set; }
        bool HasChangedThisPass { get; set; }
        
        void Update();
        HashSet<IUntypedComputed> ComputedSubscribers { get; }
        HashSet<Effect> EffectSubscribers { get; }
    }
}
