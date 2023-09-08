using System;
using System.Collections.Generic;
using UniRx;

namespace Coft.Signals
{
    public static class SignalContext
    {
        public static List<IObservable<Unit>> Observables;

        static SignalContext()
        {
            Observables = new List<IObservable<Unit>>();
        }

        // public static Signal<T> Computed<T>(Func<T> func)
        // {
        //     // IReadOnlySignal
        // }
    }
}
