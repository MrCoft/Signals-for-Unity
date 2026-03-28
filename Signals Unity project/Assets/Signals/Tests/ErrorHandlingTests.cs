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
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            Computed<int> a = null;
            var b = context.Computed(DefaultTiming, () => a!.Value + 1);
            a = context.Computed(DefaultTiming, () => b.Value + value.Value);
            var exception = Assert.Throws<Exception>(() =>
            {
                context.Update(DefaultTiming);
            });
            Assert.That(exception.Message, Does.Contain("Could not resolve signal graph; possible cycle detected; undefined behavior will follow"));
        }

        [Test]
        public void Computed_CyclicDependency_ProducesSomeValue()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            Computed<int> a = null;
            var b = context.Computed(DefaultTiming, () => a!.Value + 1);
            a = context.Computed(DefaultTiming, () => b.Value + value.Value);
            try
            {
                context.Update(DefaultTiming);
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
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            context.Effect(DefaultTiming, () => value.Value += 1);
            var exception = Assert.Throws<Exception>(() =>
            {
                context.Update(DefaultTiming);
            });
            Assert.That(exception.Message, Does.Contain("50 passes without update"));
        }

        [Test]
        public void Computed_ThrowingGetter_OtherComputedsContinue()
        {
            var context = new SignalContext();
            context.Computed<int>(DefaultTiming, () => throw new());
            var computed = context.Computed(DefaultTiming, () => 1);
            try
            {
                context.Update(DefaultTiming);
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
            var context = new SignalContext();
            var effectHasRun = false;
            context.Effect(DefaultTiming, () => throw new());
            context.Effect(DefaultTiming, () => effectHasRun = true);
            try
            {
                context.Update(DefaultTiming);
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
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            var x = 0;
            _ = context.Computed(DefaultTiming, () =>
            {
                x += 1;
                if (x >= 2) throw new();
                return value.Value * 2;
            });
            context.Update(DefaultTiming);
            value.Value += 1;
            try
            {
                context.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }

            value.Value += 1;
            try
            {
                context.Update(DefaultTiming);
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
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            var x = 0;
            context.Effect(DefaultTiming, () =>
            {
                x += 1;
                if (x >= 2) throw new();
                _ = value.Value;
            });
            context.Update(DefaultTiming);
            value.Value += 1;
            try
            {
                context.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }

            value.Value += 1;
            try
            {
                context.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }

            Assert.AreEqual(3, x);
        }
    }
}
