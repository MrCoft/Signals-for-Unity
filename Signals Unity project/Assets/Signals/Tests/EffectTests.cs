using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class EffectTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void Effect_OnCreation_DoesNotRun()
        {
            var context = new SignalContext();
            var x = 0;
            context.Effect(DefaultTiming, () => x = 1);
            Assert.AreEqual(0, x);
        }

        [Test]
        public void Effect_OnUpdate_Runs()
        {
            var context = new SignalContext();
            var x = 0;
            context.Effect(DefaultTiming, () => x = 1);
            context.Update(DefaultTiming);
            Assert.AreEqual(1, x);
        }

        [Test]
        public void Effect_SignalChange_Runs()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            var x = 0;
            context.Effect(DefaultTiming, () => x = value.Value);
            context.Update(DefaultTiming);
            value.Value = 2;
            context.Update(DefaultTiming);
            Assert.AreEqual(2, x);
        }

        [Test]
        public void Effect_UnrelatedSignalChange_DoesNotRun()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            var effectHasRun = false;
            context.Effect(DefaultTiming, () => effectHasRun = true);
            context.Update(DefaultTiming);
            effectHasRun = false;
            value.Value = 2;
            context.Update(DefaultTiming);
            Assert.That(effectHasRun, Is.False);
        }

        [Test]
        public void Effect_ConditionalRead_ChangesDependencies()
        {
            var context = new SignalContext();
            var condition = context.Signal(DefaultTiming, true);
            var value1 = context.Signal(DefaultTiming, 1);
            var value2 = context.Signal(DefaultTiming, 2);
            var x = 0;
            context.Effect(DefaultTiming, () => x = condition.Value ? value1.Value : value2.Value);
            context.Update(DefaultTiming);
            Assert.AreEqual(1, x);

            {
                condition.Value = false;
                context.Update(DefaultTiming);
                Assert.AreEqual(2, x);
            }

            {
                x = 0;
                value1.Value = 100;
                context.Update(DefaultTiming);
                Assert.AreEqual(0, x);
            }
        }

        [Test]
        public void Effect_ComputedDependency_RunsOnSignalChange()
        {
            var context = new SignalContext();
            var a = context.Signal(DefaultTiming, 1);
            var b = context.Computed(DefaultTiming, () => a.Value * 2);
            var x = 0;
            context.Effect(DefaultTiming, () => x = b.Value);
            context.Update(DefaultTiming);

            a.Value = 5;
            context.Update(DefaultTiming);

            Assert.AreEqual(10, x);
        }

        [Test]
        public void Effect_SignalAndComputedDepChange_RunsOnce()
        {
            var context = new SignalContext();
            var a = context.Signal(DefaultTiming, 1);
            var b = context.Computed(DefaultTiming, () => a.Value * 2);
            var runs = 0;
            context.Effect(DefaultTiming, () =>
            {
                var _ = a.Value + b.Value;
                runs++;
            });
            context.Update(DefaultTiming);
            runs = 0;

            a.Value = 5;
            context.Update(DefaultTiming);

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void Effect_NoChange_DoesNotRerun()
        {
            var context = new SignalContext();
            var a = context.Signal(DefaultTiming, 1);
            var runs = 0;
            context.Effect(DefaultTiming, () =>
            {
                var _ = a.Value;
                runs++;
            });
            context.Update(DefaultTiming);
            runs = 0;

            context.Update(DefaultTiming);
            context.Update(DefaultTiming);

            Assert.AreEqual(0, runs);
        }

        [Test]
        public void Effect_MultipleWritesToSameSignal_LastWriteWins()
        {
            var context = new SignalContext();
            var triggerValue = context.Signal(DefaultTiming, 0);
            var writeValue1 = context.Signal(DefaultTiming, 0);
            var writeValue2 = context.Signal(DefaultTiming, 0);
            context.Effect(DefaultTiming, () =>
            {
                writeValue1.Value = 10 + triggerValue.Value;
                writeValue2.Value = 11 + triggerValue.Value;
            });
            context.Effect(DefaultTiming, () =>
            {
                writeValue1.Value = 20 + triggerValue.Value;
            });
            context.Update(DefaultTiming);
            triggerValue.Value = 1;
            context.Update(DefaultTiming);
            Assert.AreEqual(21, writeValue1.Value);
        }
    }
}
