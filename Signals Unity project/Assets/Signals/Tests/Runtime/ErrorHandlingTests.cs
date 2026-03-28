using NUnit.Framework;
using System;

namespace Coft.Signals.Tests
{
    public class ErrorHandlingTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void Computed_CyclicDependency_ThrowsException()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            Computed<int> a = null;
            var b = signals.Computed(DefaultTiming, () => a!.Value + 1);
            a = signals.Computed(DefaultTiming, () => b.Value + value.Value);
            var exception = Assert.Throws<Exception>(() =>
            {
                signals.Update(DefaultTiming);
            });
            Assert.That(exception.Message, Does.Contain("Could not resolve signal graph; possible cycle detected; undefined behavior will follow"));
        }

        [Test]
        public void Computed_CyclicDependency_ProducesSomeValue()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            Computed<int> a = null;
            var b = signals.Computed(DefaultTiming, () => a!.Value + 1);
            a = signals.Computed(DefaultTiming, () => b.Value + value.Value);
            try
            {
                signals.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }
            Assert.That(b.Value, Is.GreaterThan(0));
        }

        [Test]
        public void Effect_InfiniteLoop_ThrowsException()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            signals.Effect(DefaultTiming, () => value.Value += 1);
            var exception = Assert.Throws<Exception>(() =>
            {
                signals.Update(DefaultTiming);
            });
            Assert.That(exception.Message, Does.Contain("50 passes without update"));
        }

        [Test]
        public void Computed_ThrowingGetter_OtherComputedsContinue()
        {
            var signals = new SignalContext();
            signals.Computed<int>(DefaultTiming, () => throw new());
            var computed = signals.Computed(DefaultTiming, () => 1);
            try
            {
                signals.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }
            Assert.AreEqual(1, computed.Value);
        }

        [Test]
        public void Effect_ThrowingAction_OtherEffectsContinue()
        {
            var signals = new SignalContext();
            var effectHasRun = false;
            signals.Effect(DefaultTiming, () => throw new());
            signals.Effect(DefaultTiming, () => effectHasRun = true);
            try
            {
                signals.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }
            Assert.That(effectHasRun, Is.True);
        }

        [Test]
        public void Computed_ThrowingGetter_RerunsWithOldDependencies()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            var x = 0;
            _ = signals.Computed(DefaultTiming, () =>
            {
                x += 1;
                if (x >= 2) throw new();
                return value.Value * 2;
            });
            signals.Update(DefaultTiming);
            value.Value += 1;
            try
            {
                signals.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }

            value.Value += 1;
            try
            {
                signals.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }

            Assert.AreEqual(3, x);
        }

        [Test]
        public void Effect_ThrowingAction_RerunsWithOldDependencies()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            var x = 0;
            signals.Effect(DefaultTiming, () =>
            {
                x += 1;
                if (x >= 2) throw new();
                _ = value.Value;
            });
            signals.Update(DefaultTiming);
            value.Value += 1;
            try
            {
                signals.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }

            value.Value += 1;
            try
            {
                signals.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }

            Assert.AreEqual(3, x);
        }
    }
}
