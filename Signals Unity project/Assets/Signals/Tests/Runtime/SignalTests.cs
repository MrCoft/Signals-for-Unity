using System.Collections.Generic;
using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class SignalTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void Signal_Write_IsDelayed()
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
        public void Signal_SameValueWrite_IsIgnored()
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
        public void Signal_MultipleReads_EventsDontMultiply()
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
        public void Signal_DifferentTimings_AreIsolated()
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
        public void Signal_CrossTiming_ComputedRunsAtItsOwnTiming()
        {
            var signals = new SignalContext();
            var source = signals.Signal(1, 10);
            var derived = signals.Computed(3, () => source.Value * 2);
            signals.Update(1);
            signals.Update(3);

            source.Value = 20;
            signals.Update(1);
            Assert.AreEqual(20, derived.Value, "computed should not have updated at timing 1");

            signals.Update(3);
            Assert.AreEqual(40, derived.Value, "computed should have updated at timing 3");
        }

        [Test]
        public void Effect_CrossTiming_EffectRunsAtItsOwnTiming()
        {
            var signals = new SignalContext();
            var source = signals.Signal(1, 10);
            var x = 0;
            signals.Effect(3, () => x = source.Value);
            signals.Update(1);
            signals.Update(3);

            source.Value = 20;
            signals.Update(1);
            Assert.AreEqual(10, x, "effect should not have run at timing 1");

            signals.Update(3);
            Assert.AreEqual(20, x, "effect should have run at timing 3");
        }

        [Test]
        public void Signal_CustomComparerSaysEqual_EffectDoesNotRun()
        {
            var comparer = new ModuloComparer(10);
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 3, comparer);
            var effectHasRun = false;
            signals.Effect(DefaultTiming, () => { var _ = value.Value; effectHasRun = true; });
            signals.Update(DefaultTiming);
            effectHasRun = false;

            value.Value = 13;
            signals.Update(DefaultTiming);

            Assert.That(effectHasRun, Is.False);
        }

        [Test]
        public void Signal_CustomComparerSaysDifferent_EffectRuns()
        {
            var comparer = new ModuloComparer(10);
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 3, comparer);
            var effectHasRun = false;
            signals.Effect(DefaultTiming, () => { var _ = value.Value; effectHasRun = true; });
            signals.Update(DefaultTiming);
            effectHasRun = false;

            value.Value = 14;
            signals.Update(DefaultTiming);

            Assert.That(effectHasRun, Is.True);
        }

        [Test]
        public void Effect_Dispose_StopsRunning()
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
            Assert.That(effectHasRun, Is.False);
        }

        private class ModuloComparer : IEqualityComparer<int>
        {
            private readonly int _modulo;
            public ModuloComparer(int modulo) => _modulo = modulo;
            public bool Equals(int x, int y) => (x % _modulo) == (y % _modulo);
            public int GetHashCode(int obj) => obj % _modulo;
        }
    }
}
