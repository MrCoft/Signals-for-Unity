using System.Collections.Generic;

namespace Coft.Signals
{
    public static class SignalList<T>
    {
        public static SignalObject<List<T>> Create(SignalContext context, int timing)
        {
            return context.Object<List<T>>(timing, CopyFrom, new());
        }

        public static void CopyFrom(List<T> listTo, List<T> listFrom)
        {
            listTo.Clear();
            listTo.AddRange(listFrom);
        }
    }
}
