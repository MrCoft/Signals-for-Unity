using NUnit.Framework;
using System.Collections.Generic;

namespace Coft.Signals.Tests
{
    public class SignalListTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void List_Write_IsDelayed()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            context.Update(DefaultTiming);

            list.GetMutable().Add(1);
            Assert.AreEqual(0, list.Count);

            context.Update(DefaultTiming);
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void List_GetMutable_RunsEffect()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            var runs = 0;
            context.Effect(DefaultTiming, () =>
            {
                _ = list.Count;
                runs++;
            });
            context.Update(DefaultTiming);
            runs = 0;

            list.GetMutable().Add(42);
            context.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void List_GetMutable_SecondCallResetsFromCommitted()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            list.GetMutable().AddRange(new[] { 1, 2, 3 });
            context.Update(DefaultTiming);

            var first = list.GetMutable();
            first.Add(4);
            Assert.AreEqual(4, first.Count);

            var second = list.GetMutable();
            Assert.AreEqual(3, second.Count);
        }

        [Test]
        public void List_MultipleWrites_RunsEffectOnce()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            var runs = 0;
            context.Effect(DefaultTiming, () =>
            {
                _ = list.Count;
                runs++;
            });
            context.Update(DefaultTiming);
            runs = 0;

            list.GetMutable().Add(1);
            list.GetMutable().Add(2);
            list.GetMutable().Add(3);
            context.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void List_Foreach_IteratesItems()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            var snapshot = new List<int>();
            context.Effect(DefaultTiming, () =>
            {
                snapshot.Clear();

                foreach (var item in list)
                {
                    snapshot.Add(item);
                }
            });
            context.Update(DefaultTiming);

            list.GetMutable().AddRange(new[] { 1, 2, 3 });
            context.Update(DefaultTiming);

            Assert.AreEqual(new List<int> { 1, 2, 3 }, snapshot);
        }

        [Test]
        public void List_GetMutable_UpdatesCountComputed()
        {
            var context = new SignalContext();
            var list = context.List<string>(DefaultTiming);
            var countComputed = context.Computed(DefaultTiming, () => list.Count);
            context.Update(DefaultTiming);
            Assert.AreEqual(0, countComputed.Value);

            list.GetMutable().AddRange(new[] { "a", "b" });
            context.Update(DefaultTiming);

            Assert.AreEqual(2, countComputed.Value);
        }

        [Test]
        public void List_UnrelatedListChange_DoesNotRunEffect()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            var other = context.List<int>(DefaultTiming);
            var runs = 0;
            context.Effect(DefaultTiming, () =>
            {
                _ = list.Count;
                runs++;
            });
            context.Update(DefaultTiming);
            runs = 0;

            other.GetMutable().Add(1);
            context.Update(DefaultTiming);

            Assert.AreEqual(0, runs);
        }

        // NOTE: Cross-timing tests

        [Test]
        public void List_CrossTiming_ComputedRunsAtItsOwnTiming()
        {
            var context = new SignalContext();
            var list = context.List<int>(1);
            var count = context.Computed(3, () => list.Count);
            context.Update(1);
            context.Update(3);

            list.GetMutable().Add(1);
            context.Update(1);
            Assert.AreEqual(0, count.Value, "computed should not have updated at timing 1");

            context.Update(3);
            Assert.AreEqual(1, count.Value, "computed should have updated at timing 3");
        }

        [Test]
        public void List_CrossTiming_EffectRunsAtItsOwnTiming()
        {
            var context = new SignalContext();
            var list = context.List<int>(1);
            var snapshot = 0;
            context.Effect(3, () => snapshot = list.Count);
            context.Update(1);
            context.Update(3);

            list.GetMutable().Add(1);
            context.Update(1);
            Assert.AreEqual(0, snapshot, "effect should not have run at timing 1");

            context.Update(3);
            Assert.AreEqual(1, snapshot, "effect should have run at timing 3");
        }
    }
}
