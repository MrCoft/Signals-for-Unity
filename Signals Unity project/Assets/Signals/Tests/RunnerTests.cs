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
    }
}
