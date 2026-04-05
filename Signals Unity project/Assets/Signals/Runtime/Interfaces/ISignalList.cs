using System.Collections.Generic;

namespace Coft.Signals
{
    public interface ISignalList<T> : IReadOnlyList<T>
    {
        List<T> Peek();
        List<T> PeekLatest();
    }
}
