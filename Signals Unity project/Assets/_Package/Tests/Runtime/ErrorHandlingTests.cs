using System;
using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class ErrorHandlingTests
    {
        private const int DefaultTiming = 0;
        
        [Test]
        public void ComputedDetectsCyclicDependencies()
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
        public void CyclicDependenciesAreResolvedByRunningLeastWrongComputed()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            Computed<int> a = null;
            var b = signals.Computed(DefaultTiming, () => a!.Value + 1);
            a = signals.Computed(DefaultTiming, () => b.Value + value.Value);
            try
            {
                signals.Update(DefaultTiming);
            } catch (Exception e)
            {
                // ignored
            }
            Assert.AreEqual(2, b.Value);
        }
        
        [Test]
        public void EffectDetectsInfiniteLoop()
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
        public void SkipsBrokenComputeds()
        {
            var signals = new SignalContext();
            var computedBroken = signals.Computed<int>(DefaultTiming, () => throw new Exception());
            var computed = signals.Computed(DefaultTiming, () => 1);
            try
            {
                signals.Update(DefaultTiming);
            } catch (Exception e)
            {
                // ignored
            }
            Assert.AreEqual(1, computed.Value);
        }
        
        [Test]
        public void SkipsBrokenEffects()
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
    }
}
