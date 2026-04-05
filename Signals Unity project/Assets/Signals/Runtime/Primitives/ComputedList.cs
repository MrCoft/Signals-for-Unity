using System;
using System.Collections;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class ComputedList<TIn, TOut> : IReadOnlyList<TOut>, IDisposable
    {
        private readonly SignalList<TOut> _list;
        private readonly Effect _effect;

        public ComputedList(SignalContext context, int timing, Func<IReadOnlyList<TIn>> sourceGetter, Func<TIn, TOut> map)
        {
            _list = context.List<TOut>(timing);

            var mapping = new Dictionary<TIn, TOut>();
            var tracker = new ListChangeTracker<TIn>(sourceGetter);

            _effect = context.Effect(timing, () =>
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

                var mutable = _list.GetMutable();
                mutable.Clear();

                for (var i = 0; i < tracker.List.Count; i++)
                {
                    mutable.Add(mapping[tracker.List[i]]);
                }
            });
        }

        public void Dispose()
        {
            _effect.Dispose();
        }

        public List<TOut> Peek()
        {
            return _list.Peek();
        }

        public List<TOut> PeekLatest()
        {
            return _list.PeekLatest();
        }

        // IReadOnlyList<T> implementation

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public TOut this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        public List<TOut>.Enumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator<TOut> IEnumerable<TOut>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
