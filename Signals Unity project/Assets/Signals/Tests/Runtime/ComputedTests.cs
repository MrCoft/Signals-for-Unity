using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class ComputedTests
    {
        private const int DefaultTiming = 0;
        
        [Test]
        public void Works()
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
        public void SortsDependencies()
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
        public void SameValueIsIgnored()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 3);
            var square = signals.Computed(DefaultTiming, () => value.Value * value.Value);
            var x = 0;
            var effect = signals.Effect(DefaultTiming, () => x = square.Value);
            var computedSideEffect = signals.Computed(DefaultTiming, () =>
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
        public void TransitiveChain_UpdatesPropagateToEnd()
        {
            var signals = new SignalContext();
            var a = signals.Signal(DefaultTiming, 1);
            var b = signals.Computed(DefaultTiming, () => a.Value * 2);
            var c = signals.Computed(DefaultTiming, () => b.Value + 1);
            signals.Update(DefaultTiming);

            a.Value = 5;
            signals.Update(DefaultTiming);

            Assert.AreEqual(10, b.Value, "b should equal signal * 2");
            Assert.AreEqual(11, c.Value, "c should equal b + 1 (transitive update)");
        }
        
        [Test]
        public void DiamondDependency_Works()
        {
            var signals = new SignalContext();
            var a = signals.Signal(DefaultTiming, 1);
            var b = signals.Computed(DefaultTiming, () => a.Value * 2);
            var c = signals.Computed(DefaultTiming, () => a.Value + b.Value);
            signals.Update(DefaultTiming);

            a.Value = 5;
            signals.Update(DefaultTiming);

            Assert.AreEqual(10, b.Value, "b should equal signal * 2");
            Assert.AreEqual(15, c.Value, "c should equal signal + b (no glitch)");
        }
    }
}
