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
    }
}
