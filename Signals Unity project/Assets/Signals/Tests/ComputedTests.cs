using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class ComputedTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void Computed_SignalChange_UpdatesValue()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            var computed = context.Computed(DefaultTiming, () => value.Value * 2);

            context.Update(DefaultTiming);
            Assert.AreEqual(2, computed.Value);

            value.Value = 2;
            context.Update(DefaultTiming);
            Assert.AreEqual(4, computed.Value);
        }

        [Test]
        public void Computed_OutOfOrderCreation_SortsDependencies()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 5);
            Computed<int> a = null;
            var b = context.Computed(DefaultTiming, () => a!.Value + 1);
            a = context.Computed(DefaultTiming, () => value.Value * -2);

            context.Update(DefaultTiming);

            Assert.AreEqual(-9, b.Value);
        }

        [Test]
        public void Computed_DeepChain_PropagatesUpdates()
        {
            var context = new SignalContext();
            var a = context.Signal(DefaultTiming, 1);
            var b = context.Computed(DefaultTiming, () => a.Value + 1);
            var c = context.Computed(DefaultTiming, () => b.Value + 1);
            var d = context.Computed(DefaultTiming, () => c.Value + 1);
            context.Update(DefaultTiming);

            a.Value = 10;
            context.Update(DefaultTiming);

            Assert.AreEqual(11, b.Value);
            Assert.AreEqual(12, c.Value);
            Assert.AreEqual(13, d.Value);
        }

        [Test]
        public void Computed_Dispose_StopsUpdates()
        {
            var context = new SignalContext();
            var a = context.Signal(DefaultTiming, 1);
            var b = context.Computed(DefaultTiming, () => a.Value * 2);
            context.Update(DefaultTiming);

            b.Dispose();
            a.Value = 5;
            context.Update(DefaultTiming);

            Assert.AreEqual(2, b.Value);
        }

        [Test]
        public void Computed_MultipleSignalDeps_UpdatesWhenEitherChanges()
        {
            var context = new SignalContext();
            var x = context.Signal(DefaultTiming, 1);
            var y = context.Signal(DefaultTiming, 2);
            var sum = context.Computed(DefaultTiming, () => x.Value + y.Value);
            context.Update(DefaultTiming);

            x.Value = 10;
            context.Update(DefaultTiming);
            Assert.AreEqual(12, sum.Value);

            y.Value = 20;
            context.Update(DefaultTiming);
            Assert.AreEqual(30, sum.Value);
        }

        [Test]
        public void Computed_NoChange_DoesNotRerun()
        {
            var context = new SignalContext();
            var a = context.Signal(DefaultTiming, 1);
            var runs = 0;
            context.Computed(DefaultTiming, () =>
            {
                runs++;
                return a.Value;
            });
            context.Update(DefaultTiming);
            var runsAfterSettle = runs;

            context.Update(DefaultTiming);
            context.Update(DefaultTiming);

            Assert.AreEqual(runsAfterSettle, runs);
        }

        [Test]
        public void Computed_SameOutputValue_DoesNotPropagate()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 3);
            var square = context.Computed(DefaultTiming, () => value.Value * value.Value);
            var x = 0;
            context.Effect(DefaultTiming, () => x = square.Value);
            context.Computed(DefaultTiming, () =>
            {
                x = square.Value;
                return square.Value;
            });
            context.Update(DefaultTiming);
            x = 0;

            value.Value = -3;
            context.Update(DefaultTiming);

            Assert.AreEqual(0, x);
        }

        [Test]
        public void Computed_TransitiveChain_PropagatesUpdates()
        {
            var context = new SignalContext();
            var a = context.Signal(DefaultTiming, 1);
            var b = context.Computed(DefaultTiming, () => a.Value * 2);
            var c = context.Computed(DefaultTiming, () => b.Value + 1);
            context.Update(DefaultTiming);

            a.Value = 5;
            context.Update(DefaultTiming);

            Assert.AreEqual(10, b.Value);
            Assert.AreEqual(11, c.Value);
        }

        [Test]
        public void Computed_DiamondDependency_ProducesCorrectValue()
        {
            var context = new SignalContext();
            var a = context.Signal(DefaultTiming, 1);
            var b = context.Computed(DefaultTiming, () => a.Value * 2);
            var c = context.Computed(DefaultTiming, () => a.Value + b.Value);
            context.Update(DefaultTiming);

            a.Value = 5;
            context.Update(DefaultTiming);

            Assert.AreEqual(10, b.Value);
            Assert.AreEqual(15, c.Value);
        }
    }
}
