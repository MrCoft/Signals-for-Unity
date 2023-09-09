using System.Collections.Generic;

namespace Coft.Signals
{
    public interface IUntypedSignal
    {
        bool IsReady { get; set; }
        bool HasChangedThisPass { get; set; }
        
        void Update();
        HashSet<IUntypedSignal> Subscribers { get; }
    }
}
