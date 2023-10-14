using System;
using System.Collections;
using System.Collections.Generic;

namespace Coft.Signals
{
    public class ReactiveList<T> : IUntypedSignal, IList<T> where T : IEquatable<T>
    {
        private T _cachedValue;
        // public bool IsDirty;
        private T _newValue;
        
        public bool IsReady { get; set; }
        public bool HasChangedThisPass { get; set; }
        public void Update()
        {
            throw new System.NotImplementedException();
        }

        public HashSet<IUntypedComputed> ComputedSubscribers { get; }
        public HashSet<Effect> EffectSubscribers { get; }

        public enum DependencyType
        {
            All,
            Index,
            Find,
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public int Count { get; }
        public bool IsReadOnly { get; }
        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public T this[int index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
