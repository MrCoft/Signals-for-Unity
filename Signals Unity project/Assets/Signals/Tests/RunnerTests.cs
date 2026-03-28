using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class RunnerTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void Runner_ComputedCreatedBeforeDep_FirstRunSeesStaleValue()
        {
            var context = new SignalContext();
            Computed<int> a = null;
            int? firstWrite = null;
            var b = context.Computed(DefaultTiming, () =>
            {
                var value = a!.Value;
                if (firstWrite == null)
                {
                    firstWrite = value;
                }

                return value + 1;
            });
            a = context.Computed(DefaultTiming, () => 1);

            context.Update(DefaultTiming);

            Assert.AreEqual(2, b.Value);
            Assert.AreEqual(0, firstWrite);
        }

        [Test]
        public void Runner_SignalOnlyDeps_RunsOnce()
        {
            var context = new SignalContext();
            var value = context.Signal(DefaultTiming, 1);
            var x = 0;
            context.Computed(DefaultTiming, () =>
            {
                x += 1;
                return value.Value * 2;
            });

            context.Update(DefaultTiming);

            Assert.AreEqual(1, x);
        }

        [Test]
        public void Runner_DepChangesOnFirstPass_CausesRerun()
        {
            var context = new SignalContext();
            var condition = context.Signal(DefaultTiming, true);
            Computed<bool> computedCondition = null;
            Computed<int> resultTrue = null;
            var numberOfRuns = 0;
            _ = context.Computed(DefaultTiming, () =>
            {
                numberOfRuns += 1;
                return computedCondition!.Value ? resultTrue!.Value : 0;
            });
            resultTrue = context.Computed(DefaultTiming, () => computedCondition!.Value ? 2 : 1);
            computedCondition = context.Computed(DefaultTiming, () => condition.Value);

            context.Update(DefaultTiming);

            Assert.AreEqual(3, numberOfRuns);
        }

        [Test]
        public void Runner_SignalOverwrittenByEffect_WaitsForConsistentState()
        {
            var context = new SignalContext();
            var readValue = context.Signal(DefaultTiming, 0);
            var writeValue = context.Signal(DefaultTiming, 0);
            var x = 0;
            context.Effect(DefaultTiming, () => writeValue.Value = readValue.Value);
            context.Effect(DefaultTiming, () =>
            {
                _ = writeValue.Value;
                x += 1;
            });
            context.Update(DefaultTiming);
            x = 0;

            readValue.Value = 10;
            writeValue.Value = 9;
            context.Update(DefaultTiming);

            Assert.AreEqual(1, x);
        }
    }
}
