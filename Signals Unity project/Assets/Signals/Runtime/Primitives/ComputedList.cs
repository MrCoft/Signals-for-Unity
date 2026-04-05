using System;
using System.Collections.Generic;

namespace Coft.Signals
{
    public static class ComputedList
    {
        public static SignalList<TOut> Create<TIn, TOut>(
            SignalContext context,
            int timing,
            Func<IReadOnlyList<TIn>> sourceGetter,
            Func<TIn, TOut> map)
        {
            var result = context.List<TOut>(timing);
            var mapping = new Dictionary<TIn, TOut>();
            var tracker = new ListChangeTracker<TIn>(sourceGetter);

            context.Effect(timing, () =>
            {
                tracker.Update();

                foreach (var item in tracker.Removed)
                {
                    mapping.Remove(item);
                }

                foreach (var item in tracker.Added)
                {
                    mapping[item] = map(item);
                }

                var mutable = result.GetMutable();
                mutable.Clear();

                for (var i = 0; i < tracker.List.Count; i++)
                {
                    mutable.Add(mapping[tracker.List[i]]);
                }
            });

            return result;
        }
    }
}
