using System.Collections.Generic;
using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class OneTimeEffectTests
    {
        private const int DefaultTiming = 0;

        [Test]
        public void OneTimeEffect_RunsAfterNormalEffects()
        {
            var context = new SignalContext();
            var order = new List<string>();
            context.Effect(DefaultTiming, () => order.Add("effect"));
            context.OneTimeEffect(DefaultTiming, () => order.Add("oneTime"));

            context.Update(DefaultTiming);

            Assert.AreEqual(new[] { "effect", "oneTime" }, order.ToArray());
        }

        [Test]
        public void OneTimeEffect_WhenItWritesSignal_PropagatesChange()
        {
            var context = new SignalContext();
            var trigger = context.Signal(DefaultTiming, 0);
            var observed = 0;
            context.Effect(DefaultTiming, () => observed = trigger.Value);
            context.OneTimeEffect(DefaultTiming, () => trigger.Value = 42);

            context.Update(DefaultTiming);

            Assert.AreEqual(42, observed);
        }

        [Test]
        public void OneTimeEffect_BetweenTwo_ReactiveSettlesBetween()
        {
            var context = new SignalContext();
            var source = context.Signal(DefaultTiming, 0);
            var doubled = context.Computed(DefaultTiming, () => source.Value * 2);
            var seen = 0;

            context.OneTimeEffect(DefaultTiming, () => source.Value = 7);
            context.OneTimeEffect(DefaultTiming, () => seen = doubled.Value);

            context.Update(DefaultTiming);

            Assert.AreEqual(14, seen);
        }
    }
}
