using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class RunnerTests
    {
        private const int DefaultTiming = 0;
        
        [Test]
        public void FirstRunUsesWrongValues()
        {
            var signals = new SignalContext();
            var a = signals.Computed(DefaultTiming, () => 1);
            int? firstWrite = null;
            var b = signals.Computed(DefaultTiming, () =>
            {
                var value = a.Value;
                if (firstWrite == null)
                {
                    firstWrite = value;
                }

                return value + 1;
            });
            a.Dispose();
            a = signals.Computed(DefaultTiming, () => 1);
            signals.Update(DefaultTiming);
            Assert.AreEqual(2, b.Value);
            Assert.AreEqual(0, firstWrite);
        }

        [Test]
        public void DependOnlyOnSignalsAreRunOnce()
        {
            var signals = new SignalContext();
            var value = signals.Signal(DefaultTiming, 1);
            var x = 0;
            var computed = signals.Computed(DefaultTiming, () =>
            {
                x += 1;
                return value.Value * 2;
            });
            signals.Update(DefaultTiming);
            Assert.AreEqual(1, x);
        }

        [Test]
        public void DependencyChangeOnFirstPassCausesRerun()
        {
            var signals = new SignalContext();
            var condition = signals.Signal(DefaultTiming, true);
            Computed<bool> computedCondition = null;
            Computed<int> resultTrue = null;
            var numberOfRuns = 0;
            var compositeComputed = signals.Computed(DefaultTiming, () =>
            {
                numberOfRuns += 1;
                return computedCondition!.Value ? resultTrue!.Value : 0;
            });
            resultTrue = signals.Computed(DefaultTiming, () => computedCondition!.Value ? 2 : 1);
            computedCondition = signals.Computed(DefaultTiming, () => condition.Value);
            signals.Update(DefaultTiming);
            Assert.AreEqual(3, numberOfRuns);
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
    }
}
