using System;
using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class EffectTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void DoesntRunImmediately()
        {
            var signals = new SignalContext();
            var x = 0;
            signals.Effect(DefaultTiming, () => x = 1);
            Assert.AreEqual(0, x);
        }
        
        [Test]
        public void RunsOnUpdate()
        {
            var signals = new SignalContext();
            var x = 0;
            signals.Effect(DefaultTiming, () => x = 1);
            signals.Update(DefaultTiming);
            Assert.AreEqual(1, x);
        }

        [Test]
        public void RunsOnChange()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            var x = 0;
            signals.Effect(DefaultTiming, () => x = value.Value);
            signals.Update(DefaultTiming);
            value.Value = 2;
            signals.Update(DefaultTiming);
            Assert.AreEqual(2, x);
        }

        [Test]
        public void DoesntRunOnUnrelatedChange()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            var effectHasRun = false;
            signals.Effect(DefaultTiming, () => effectHasRun = true);
            signals.Update(DefaultTiming);
            effectHasRun = false;
            value.Value = 2;
            signals.Update(DefaultTiming);
            Assert.AreEqual(false, effectHasRun);
        }

        [Test]
        public void ChangesDependencies()
        {
            var signals = new SignalContext();
            var condition = signals.Signal(DefaultTiming, true);
            var value1 = signals.Signal(DefaultTiming, 1);
            var value2 = signals.Signal(DefaultTiming, 2);
            var x = 0;
            signals.Effect(DefaultTiming, () => x = condition.Value ? value1.Value : value2.Value);
            signals.Update(DefaultTiming);
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
        
        [Test]
        public void DetectsInfiniteLoop()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            signals.Effect(DefaultTiming, () => value.Value += 1);
            Assert.Throws<Exception>(() =>
            {
                signals.Update(DefaultTiming);
            });
        }

        [Test]
        public void SkipsBroken()
        {
            var signals = new SignalContext();
            var effectHasRun = false;
            signals.Effect(DefaultTiming, () => throw new Exception());
            signals.Effect(DefaultTiming, () => effectHasRun = true);
            try
            {
                signals.Update(DefaultTiming);
            }
            catch (Exception)
            {
                // ignored
            }
            
            Assert.AreEqual(true, effectHasRun);
        }

        [Test]
        public void WaitsWhenDependencyOverwrittenByAnotherEffect()
        {
            var signals = new SignalContext();
            var readValue = signals.Signal(DefaultTiming, 0);
            var writeValue = signals.Signal(DefaultTiming, 0);
            var x = 0;
            signals.Effect(DefaultTiming, () => writeValue.Value = readValue.Value);
            signals.Effect(DefaultTiming, () =>
            {
                var read = writeValue.Value;
                x += 1;
            });
            signals.Update(DefaultTiming);
            x = 0;
            readValue.Value = 10;
            writeValue.Value = 9;
            signals.Update(DefaultTiming);
            Assert.AreEqual(1, x);
        }

        [Test]
        public void MultipleEffectsOverwrite()
        {
            var signals = new SignalContext();
            var triggerValue = signals.Signal(DefaultTiming, 0);
            var writeValue1 = signals.Signal(DefaultTiming, 0);
            var writeValue2 = signals.Signal(DefaultTiming, 0);
            signals.Effect(DefaultTiming, () =>
            {
                writeValue1.Value = 10 + triggerValue.Value;
                writeValue2.Value = 11 + triggerValue.Value;
            });
            signals.Effect(DefaultTiming, () =>
            {
                writeValue1.Value = 20 + triggerValue.Value;
            });
            signals.Update(DefaultTiming);
            triggerValue.Value = 1;
            signals.Update(DefaultTiming);
            Assert.AreEqual(21, writeValue1.Value);
        }
    }
}
