using System;
using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class ComputedTests
    {
        private const int DefaultTiming = 0;
        
        [Test]
        public void ComputedWorks()
        {
            var signals = new SignalManager();
            var value = signals.Signal(DefaultTiming, 1);
            var computed = signals.Computed(DefaultTiming, () => value.Value * 2);
            signals.Update(DefaultTiming);
            Assert.AreEqual(2, computed.Value);
            value.Value = 2;
            signals.Update(DefaultTiming);
            Assert.AreEqual(4, computed.Value);
        }
        
        [Test]
        public void ComputedSortsDependencies()
        {
            var signals = new SignalManager();
            var value = signals.Signal(DefaultTiming, 1);
            var a = signals.Computed(DefaultTiming, () => value.Value * 2);
            var b = signals.Computed(DefaultTiming, () => a.Value + 1);
            a = signals.Computed(DefaultTiming, () => value.Value * -2);
            value.Value = 5;
            signals.Update(DefaultTiming);
            Assert.AreEqual(-9, b.Value);
        }
        
        [Test]
        public void ComputedDetectsCyclicDependencies()
        {
            var signals = new SignalManager();
            var value = signals.Signal(DefaultTiming, 1);
            var a = signals.Computed(DefaultTiming, () => value.Value);
            var b = signals.Computed(DefaultTiming, () => a.Value + 1);
            a = signals.Computed(DefaultTiming, () => b.Value + value.Value);
            value.Value = 2;
            Assert.Throws<Exception>(() =>
            {
                signals.Update(DefaultTiming);
            });
        }

        [Test]
        public void ComputedSameValueIsIgnored()
        {
            var signals = new SignalManager();
            var value = signals.Signal(DefaultTiming, 1);
            var square = signals.Computed(DefaultTiming, () => value.Value * value.Value);
            var x = 0;
            signals.Effect(DefaultTiming, () => x = square.Value);
            value.Value = 3;
            signals.Update(DefaultTiming);
            x = 0;
            value.Value = -3;
            signals.Update(DefaultTiming);
            Assert.AreEqual(0, x);
        }
    }
}
