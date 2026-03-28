using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class ComputedTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void Computed_SignalChange_UpdatesValue()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            var computed = signals.Computed(DefaultTiming, () => value.Value * 2);
            signals.Update(DefaultTiming);
            Assert.AreEqual(2, computed.Value);
            value.Value = 2;
            signals.Update(DefaultTiming);
            Assert.AreEqual(4, computed.Value);
        }

        [Test]
        public void Computed_OutOfOrderCreation_SortsDependencies()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 5);
            Computed<int> a = null;
            var b = signals.Computed(DefaultTiming, () => a!.Value + 1);
            a = signals.Computed(DefaultTiming, () => value.Value * -2);
            signals.Update(DefaultTiming);
            Assert.AreEqual(-9, b.Value);
        }

        [Test]
        public void Computed_DeepChain_PropagatesUpdates()
        {
            var signals = new SignalContext();
            var a = signals.Signal(DefaultTiming, 1);
            var b = signals.Computed(DefaultTiming, () => a.Value + 1);
            var c = signals.Computed(DefaultTiming, () => b.Value + 1);
            var d = signals.Computed(DefaultTiming, () => c.Value + 1);
            signals.Update(DefaultTiming);

            a.Value = 10;
            signals.Update(DefaultTiming);

            Assert.AreEqual(11, b.Value);
            Assert.AreEqual(12, c.Value);
            Assert.AreEqual(13, d.Value);
        }

        [Test]
        public void Computed_Dispose_StopsUpdates()
        {
            var signals = new SignalContext();
            var a = signals.Signal(DefaultTiming, 1);
            var b = signals.Computed(DefaultTiming, () => a.Value * 2);
            signals.Update(DefaultTiming);

            b.Dispose();
            a.Value = 5;
            signals.Update(DefaultTiming);

            Assert.AreEqual(2, b.Value);
        }

        [Test]
        public void Computed_MultipleSignalDeps_UpdatesWhenEitherChanges()
        {
            var signals = new SignalContext();
            var x = signals.Signal(DefaultTiming, 1);
            var y = signals.Signal(DefaultTiming, 2);
            var sum = signals.Computed(DefaultTiming, () => x.Value + y.Value);
            signals.Update(DefaultTiming);

            x.Value = 10;
            signals.Update(DefaultTiming);
            Assert.AreEqual(12, sum.Value);

            y.Value = 20;
            signals.Update(DefaultTiming);
            Assert.AreEqual(30, sum.Value);
        }

        [Test]
        public void Computed_NoChange_DoesNotRerun()
        {
            var signals = new SignalContext();
            var a = signals.Signal(DefaultTiming, 1);
            var runs = 0;
            signals.Computed(DefaultTiming, () => { runs++; return a.Value; });
            signals.Update(DefaultTiming);
            var runsAfterSettle = runs;

            signals.Update(DefaultTiming);
            signals.Update(DefaultTiming);

            Assert.AreEqual(runsAfterSettle, runs);
        }

        [Test]
        public void Computed_SameOutputValue_DoesNotPropagate()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 3);
            var square = signals.Computed(DefaultTiming, () => value.Value * value.Value);
            var x = 0;
            signals.Effect(DefaultTiming, () => x = square.Value);
            signals.Computed(DefaultTiming, () =>
            {
                x = square.Value;
                return square.Value;
            });
            signals.Update(DefaultTiming);
            x = 0;
            value.Value = -3;
            signals.Update(DefaultTiming);
            Assert.AreEqual(0, x);
        }

        [Test]
        public void Computed_TransitiveChain_PropagatesUpdates()
        {
            var signals = new SignalContext();
            var a = signals.Signal(DefaultTiming, 1);
            var b = signals.Computed(DefaultTiming, () => a.Value * 2);
            var c = signals.Computed(DefaultTiming, () => b.Value + 1);
            signals.Update(DefaultTiming);

            a.Value = 5;
            signals.Update(DefaultTiming);

            Assert.AreEqual(10, b.Value);
            Assert.AreEqual(11, c.Value);
        }

        [Test]
        public void Computed_DiamondDependency_ProducesCorrectValue()
        {
            var signals = new SignalContext();
            var a = signals.Signal(DefaultTiming, 1);
            var b = signals.Computed(DefaultTiming, () => a.Value * 2);
            var c = signals.Computed(DefaultTiming, () => a.Value + b.Value);
            signals.Update(DefaultTiming);

            a.Value = 5;
            signals.Update(DefaultTiming);

            Assert.AreEqual(10, b.Value);
            Assert.AreEqual(15, c.Value);
        }
    }
}
