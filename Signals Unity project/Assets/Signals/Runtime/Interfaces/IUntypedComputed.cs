using System.Collections.Generic;

namespace Coft.Signals
{
    public interface IUntypedComputed : IUntypedSignal
    {
        int Timing { get; }
        HashSet<IUntypedSignal> Dependencies { get; }

        void Run();
    }
}
