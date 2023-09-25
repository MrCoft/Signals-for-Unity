using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class SignalTests
    {
        private const int DefaultTiming = 0;
        
        [Test]
        public void WriteIsDelayed()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            Assert.AreEqual(1, value.Value);
            value.Value = 2;
            Assert.AreEqual(1, value.Value);
            signals.Update(DefaultTiming);
            Assert.AreEqual(2, value.Value);
        }

        [Test]
        public void SameValueWriteIsIgnored()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            var x = 0;
            signals.Effect(DefaultTiming, () => x = value.Value);
            signals.Update(DefaultTiming);
            x = 0;
            value.Value = 1;
            signals.Update(DefaultTiming);
            Assert.AreEqual(0, x);
        }
        
        [Test]
        public void EventsDontMultiply()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            var computed = signals.Computed(DefaultTiming, () => value.Value * 2);
            var x = 0;
            signals.Effect(DefaultTiming, () =>
            {
                var read = value.Value;
                read = computed.Value;
                x += 1;
            });
            signals.Update(DefaultTiming);
            Assert.AreEqual(1, x);
        }
        
        [Test]
        public void Dispose()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            var effectHasRun = false;
            var effect = signals.Effect(DefaultTiming, () =>
            {
                var read = value.Value;
                effectHasRun = true;
            });
            signals.Update(DefaultTiming);
            effect.Dispose();
            value.Value = 2;
            effectHasRun = false;
            signals.Update(DefaultTiming);
            Assert.AreEqual(false, effectHasRun);
        }
    }
}
