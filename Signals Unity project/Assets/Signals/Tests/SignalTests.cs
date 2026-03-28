using NUnit.Framework;
using System.Collections.Generic;

namespace Coft.Signals.Tests
{
    public class SignalTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void Signal_Write_IsDelayed()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            Assert.AreEqual(1, value.Value);

            value.Value = 2;
            Assert.AreEqual(1, value.Value);

            context.Update(DefaultTiming);
            Assert.AreEqual(2, value.Value);
        }

        [Test]
        public void Signal_SameValueWrite_IsIgnored()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            var x = 0;
            context.Effect(DefaultTiming, () => x = value.Value);
            context.Update(DefaultTiming);

            x = 0;
            value.Value = 1;
            context.Update(DefaultTiming);

            Assert.AreEqual(0, x);
        }

        [Test]
        public void Signal_MultipleReads_EventsDontMultiply()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            var computed = context.Computed(DefaultTiming, () => value.Value * 2);
            var x = 0;
            context.Effect(DefaultTiming, () =>
            {
                _ = value.Value;
                _ = computed.Value;
                x += 1;
            });
            context.Update(DefaultTiming);

            Assert.AreEqual(1, x);
        }

        [Test]
        public void Signal_DifferentTimings_AreIsolated()
        {
            var context = new SignalContext();
            var a = context.Signal(0, 1);
            var b = context.Signal(1, 1);
            var x = 0;
            var y = 0;
            context.Effect(0, () => x = a.Value);
            context.Effect(1, () => y = b.Value);
            context.Update(0);
            context.Update(1);

            a.Value = 10;
            b.Value = 20;

            context.Update(0);
            Assert.AreEqual(10, x, "timing 0 effect should have run");
            Assert.AreEqual(1, y, "timing 1 effect should not have run yet");

            context.Update(1);
            Assert.AreEqual(20, y, "timing 1 effect should now have run");
        }

        [Test]
        public void Signal_CrossTiming_ComputedRunsAtItsOwnTiming()
        {
            var context = new SignalContext();
            var source = context.Signal(1, 10);
            var derived = context.Computed(3, () => source.Value * 2);
            context.Update(1);
            context.Update(3);

            source.Value = 20;
            context.Update(1);
            Assert.AreEqual(20, derived.Value, "computed should not have updated at timing 1");

            context.Update(3);
            Assert.AreEqual(40, derived.Value, "computed should have updated at timing 3");
        }

        [Test]
        public void Effect_CrossTiming_EffectRunsAtItsOwnTiming()
        {
            var context = new SignalContext();
            var source = context.Signal(1, 10);
            var x = 0;
            context.Effect(3, () => x = source.Value);
            context.Update(1);
            context.Update(3);

            source.Value = 20;
            context.Update(1);
            Assert.AreEqual(10, x, "effect should not have run at timing 1");

            context.Update(3);
            Assert.AreEqual(20, x, "effect should have run at timing 3");
        }

        private class ModuloComparer : IEqualityComparer<int>
        {
            private readonly int _modulo;

            public ModuloComparer(int modulo)
            {
                _modulo = modulo;
            }

            public bool Equals(int x, int y)
            {
                return x % _modulo == y % _modulo;
            }

            public int GetHashCode(int obj)
            {
                return obj % _modulo;
            }
        }

        [Test]
        public void Signal_CustomComparerSaysEqual_EffectDoesNotRun()
        {
            var context = new SignalContext();
            var comparer = new ModuloComparer(10);
            var value = context.Signal(DefaultTiming, 3, comparer);
            var effectHasRun = false;
            context.Effect(DefaultTiming, () =>
            {
                _ = value.Value;
                effectHasRun = true;
            });
            context.Update(DefaultTiming);
            effectHasRun = false;

            value.Value = 13;
            context.Update(DefaultTiming);

            Assert.That(effectHasRun, Is.False);
        }

        [Test]
        public void Signal_CustomComparerSaysDifferent_EffectRuns()
        {
            var context = new SignalContext();
            var comparer = new ModuloComparer(10);
            var value = context.Signal(DefaultTiming, 3, comparer);
            var effectHasRun = false;
            context.Effect(DefaultTiming, () =>
            {
                _ = value.Value;
                effectHasRun = true;
            });
            context.Update(DefaultTiming);
            effectHasRun = false;

            value.Value = 14;
            context.Update(DefaultTiming);

            Assert.That(effectHasRun, Is.True);
        }

        [Test]
        public void Effect_Dispose_StopsRunning()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            var effectHasRun = false;
            var effect = context.Effect(DefaultTiming, () =>
            {
                _ = value.Value;
                effectHasRun = true;
            });
            context.Update(DefaultTiming);

            effect.Dispose();
            value.Value = 2;
            effectHasRun = false;
            context.Update(DefaultTiming);

            Assert.That(effectHasRun, Is.False);
        }
    }
}
