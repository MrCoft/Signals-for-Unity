using System.Collections.Generic;

namespace Coft.Signals
{
    public interface IUntypedComputed : IUntypedSignal
    {
        HashSet<IUntypedSignal> Dependencies { get; }

        void Run();
    }
}
