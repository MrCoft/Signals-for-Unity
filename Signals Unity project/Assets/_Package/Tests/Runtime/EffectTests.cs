using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class EffectTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void EffectRunsImmediately()
        {
            var signals = new SignalManager();
            var x = 0;
            signals.Effect(DefaultTiming, () => x = 1);
            Assert.AreEqual(1, x);
        }

        [Test]
        public void EffectRunsOnChange()
        {
            var signals = new SignalManager();
            var value = signals.CreateSignal(DefaultTiming, 1);
            var x = 0;
            signals.Effect(DefaultTiming, () => x = value.Value);
            value.Value = 2;
            signals.Update(DefaultTiming);
            Assert.AreEqual(2, x);
        }

        [Test]
        public void EffectDoesntRunOnUnrelatedChange()
        {
            var signals = new SignalManager();
            var value = signals.CreateSignal(DefaultTiming, 1);
            var effectHasRun = false;
            signals.Effect(DefaultTiming, () => effectHasRun = true);
            effectHasRun = false;
            value.Value = 2;
            signals.Update(DefaultTiming);
            Assert.AreEqual(false, effectHasRun);
        }

        [Test]
        public void EffectChangesDependencies()
        {
            var signals = new SignalManager();
            var condition = signals.CreateSignal(DefaultTiming, true);
            var value1 = signals.CreateSignal(DefaultTiming, 1);
            var value2 = signals.CreateSignal(DefaultTiming, 2);
            var x = 0;
            signals.Effect(DefaultTiming, () => x = condition.Value ? value1.Value : value2.Value);
            Assert.AreEqual(1, x);

            {
                condition.Value = false;
                signals.Update(DefaultTiming);
                Assert.AreEqual(2, x);
            }

            {
                x = 0;
                value1.Value = 100;
                signals.Update(DefaultTiming);
                Assert.AreEqual(0, x);
            }
        }
    }
}
