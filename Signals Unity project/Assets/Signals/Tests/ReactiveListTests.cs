using NUnit.Framework;
using System.Collections.Generic;

namespace Coft.Signals.Tests
{
    public class ReactiveListTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void List_Foreach_IteratesAddedItems()
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

            list.Add(1);
            list.Add(2);
            list.Add(3);
            context.Update(DefaultTiming);

            Assert.AreEqual(new List<int>
            {
                1,
                2,
                3
            }, snapshot);
        }

        [Test]
        public void List_Add_RunsEffect()
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

            list.Add(42);
            context.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void List_Remove_RunsEffect()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            list.Add(1);
            list.Add(2);
            var runs = 0;
            context.Effect(DefaultTiming, () =>
            {
                _ = list.Count;
                runs++;
            });
            context.Update(DefaultTiming);
            runs = 0;

            list.Remove(1);
            context.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void List_RemoveAt_RunsEffect()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            list.Add(10);
            list.Add(20);
            var runs = 0;
            context.Effect(DefaultTiming, () =>
            {
                _ = list.Count;
                runs++;
            });
            context.Update(DefaultTiming);
            runs = 0;

            list.RemoveAt(0);
            context.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void List_Insert_RunsEffect()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            list.Add(1);
            list.Add(3);
            var runs = 0;
            context.Effect(DefaultTiming, () =>
            {
                _ = list.Count;
                runs++;
            });
            context.Update(DefaultTiming);
            runs = 0;

            list.Insert(1, 2);
            context.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
            Assert.AreEqual(3, list.Count);
        }

        [Test]
        public void List_IndexSet_RunsEffect()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            list.Add(1);
            var runs = 0;
            context.Effect(DefaultTiming, () =>
            {
                _ = list.Count;
                runs++;
            });
            context.Update(DefaultTiming);
            runs = 0;

            list[0] = 99;
            context.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void List_Clear_RunsEffect()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            list.Add(1);
            list.Add(2);
            var runs = 0;
            context.Effect(DefaultTiming, () =>
            {
                _ = list.Count;
                runs++;
            });
            context.Update(DefaultTiming);
            runs = 0;

            list.Clear();
            context.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void List_ClearWhenEmpty_DoesNotRunEffect()
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

            list.Clear();
            context.Update(DefaultTiming);

            Assert.AreEqual(0, runs);
        }

        [Test]
        public void List_MultipleChanges_RunsEffectOnce()
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

            list.Add(1);
            list.Add(2);
            list.Add(3);
            context.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
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

            other.Add(1);
            context.Update(DefaultTiming);

            Assert.AreEqual(0, runs);
        }

        [Test]
        public void List_Add_UpdatesCountComputed()
        {
            var context = new SignalContext();
            var list = context.List<string>(DefaultTiming);
            var countComputed = context.Computed(DefaultTiming, () => list.Count);
            context.Update(DefaultTiming);
            Assert.AreEqual(0, countComputed.Value);

            list.Add("a");
            list.Add("b");
            context.Update(DefaultTiming);

            Assert.AreEqual(2, countComputed.Value);
        }

        [Test]
        public void List_IndexSet_UpdatesIndexComputed()
        {
            var context = new SignalContext();
            var list = context.List<string>(DefaultTiming);
            list.Add("hello");
            var first = context.Computed(DefaultTiming, () => list.Count > 0 ? list[0] : "");
            context.Update(DefaultTiming);
            Assert.AreEqual("hello", first.Value);

            list[0] = "world";
            context.Update(DefaultTiming);

            Assert.AreEqual("world", first.Value);
        }

        [Test]
        public void List_Foreach_TracksChanges()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            var sum = 0;
            context.Effect(DefaultTiming, () =>
            {
                sum = 0;

                foreach (var item in list)
                {
                    sum += item;
                }
            });
            context.Update(DefaultTiming);

            list.Add(10);
            list.Add(20);
            list.Add(30);
            context.Update(DefaultTiming);

            Assert.AreEqual(60, sum);
        }

        [Test]
        public void List_Contains_TracksChanges()
        {
            var context = new SignalContext();
            var list = context.List<int>(DefaultTiming);
            var hasThree = false;
            context.Effect(DefaultTiming, () => hasThree = list.Contains(3));
            context.Update(DefaultTiming);
            Assert.That(hasThree, Is.False);

            list.Add(3);
            context.Update(DefaultTiming);

            Assert.That(hasThree, Is.True);
        }
    }
}
