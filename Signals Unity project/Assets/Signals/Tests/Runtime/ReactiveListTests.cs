using System.Collections.Generic;
using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class ReactiveListTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void Works()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            var snapshot = new List<int>();
            signals.Effect(DefaultTiming, () =>
            {
                snapshot.Clear();
                foreach (var item in list) snapshot.Add(item);
            });
            signals.Update(DefaultTiming);

            list.Add(1);
            list.Add(2);
            list.Add(3);
            signals.Update(DefaultTiming);

            Assert.AreEqual(new List<int> { 1, 2, 3 }, snapshot);
        }

        [Test]
        public void EffectRunsOnAdd()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            var runs = 0;
            signals.Effect(DefaultTiming, () => { var _ = list.Count; runs++; });
            signals.Update(DefaultTiming);
            runs = 0;

            list.Add(42);
            signals.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void EffectRunsOnRemove()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            list.Add(1);
            list.Add(2);
            var runs = 0;
            signals.Effect(DefaultTiming, () => { var _ = list.Count; runs++; });
            signals.Update(DefaultTiming);
            runs = 0;

            list.Remove(1);
            signals.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void EffectRunsOnRemoveAt()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            list.Add(10);
            list.Add(20);
            var runs = 0;
            signals.Effect(DefaultTiming, () => { var _ = list.Count; runs++; });
            signals.Update(DefaultTiming);
            runs = 0;

            list.RemoveAt(0);
            signals.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void EffectRunsOnInsert()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            list.Add(1);
            list.Add(3);
            var runs = 0;
            signals.Effect(DefaultTiming, () => { var _ = list.Count; runs++; });
            signals.Update(DefaultTiming);
            runs = 0;

            list.Insert(1, 2);
            signals.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
            Assert.AreEqual(3, list.Count);
        }

        [Test]
        public void EffectRunsOnIndexSet()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            list.Add(1);
            var runs = 0;
            signals.Effect(DefaultTiming, () => { var _ = list.Count; runs++; });
            signals.Update(DefaultTiming);
            runs = 0;

            list[0] = 99;
            signals.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void EffectRunsOnClear()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            list.Add(1);
            list.Add(2);
            var runs = 0;
            signals.Effect(DefaultTiming, () => { var _ = list.Count; runs++; });
            signals.Update(DefaultTiming);
            runs = 0;

            list.Clear();
            signals.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void ClearOnEmptyListDoesntFire()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            var runs = 0;
            signals.Effect(DefaultTiming, () => { var _ = list.Count; runs++; });
            signals.Update(DefaultTiming);
            runs = 0;

            list.Clear();
            signals.Update(DefaultTiming);

            Assert.AreEqual(0, runs);
        }

        [Test]
        public void MultipleChangesInOneFrameFireEffectOnce()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            var runs = 0;
            signals.Effect(DefaultTiming, () => { var _ = list.Count; runs++; });
            signals.Update(DefaultTiming);
            runs = 0;

            list.Add(1);
            list.Add(2);
            list.Add(3);
            signals.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void EffectDoesntRunWhenUnrelated()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            var other = signals.List<int>(DefaultTiming);
            var runs = 0;
            signals.Effect(DefaultTiming, () => { var _ = list.Count; runs++; });
            signals.Update(DefaultTiming);
            runs = 0;

            other.Add(1);
            signals.Update(DefaultTiming);

            Assert.AreEqual(0, runs);
        }

        [Test]
        public void ComputedReadsCount()
        {
            var signals = new SignalContext();
            var list = signals.List<string>(DefaultTiming);
            var countComputed = signals.Computed(DefaultTiming, () => list.Count);
            signals.Update(DefaultTiming);
            Assert.AreEqual(0, countComputed.Value);

            list.Add("a");
            list.Add("b");
            signals.Update(DefaultTiming);

            Assert.AreEqual(2, countComputed.Value);
        }

        [Test]
        public void ComputedReadsIndex()
        {
            var signals = new SignalContext();
            var list = signals.List<string>(DefaultTiming);
            list.Add("hello");
            var first = signals.Computed(DefaultTiming, () => list.Count > 0 ? list[0] : "");
            signals.Update(DefaultTiming);
            Assert.AreEqual("hello", first.Value);

            list[0] = "world";
            signals.Update(DefaultTiming);

            Assert.AreEqual("world", first.Value);
        }

        [Test]
        public void ForeachInEffectTracksChanges()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            var sum = 0;
            signals.Effect(DefaultTiming, () =>
            {
                sum = 0;
                foreach (var item in list) sum += item;
            });
            signals.Update(DefaultTiming);

            list.Add(10);
            list.Add(20);
            list.Add(30);
            signals.Update(DefaultTiming);

            Assert.AreEqual(60, sum);
        }

        [Test]
        public void ContainsTracksChanges()
        {
            var signals = new SignalContext();
            var list = signals.List<int>(DefaultTiming);
            var hasThree = false;
            signals.Effect(DefaultTiming, () => hasThree = list.Contains(3));
            signals.Update(DefaultTiming);
            Assert.AreEqual(false, hasThree);

            list.Add(3);
            signals.Update(DefaultTiming);

            Assert.AreEqual(true, hasThree);
        }
    }
}
