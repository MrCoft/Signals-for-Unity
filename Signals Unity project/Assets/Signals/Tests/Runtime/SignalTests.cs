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
        public void TimingIsolation()
        {
            var signals = new SignalContext();
            var a = signals.Signal(0, 1);
            var b = signals.Signal(1, 1);
            var x = 0;
            var y = 0;
            signals.Effect(0, () => x = a.Value);
            signals.Effect(1, () => y = b.Value);
            signals.Update(0);
            signals.Update(1);

            a.Value = 10;
            b.Value = 20;

            signals.Update(0);
            Assert.AreEqual(10, x, "timing 0 effect should have run");
            Assert.AreEqual(1,  y, "timing 1 effect should not have run yet");

            signals.Update(1);
            Assert.AreEqual(20, y, "timing 1 effect should now have run");
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
