using NUnit.Framework;

namespace Coft.Signals.Tests
{
    public class SignalsTests
    {
        private const int DefaultTiming = 0;
        
        [Test]
        public void WriteIsDelayed()
        {
            var signals = new SignalManager();
            var value = signals.CreateSignal(DefaultTiming, 1);
            Assert.AreEqual(1, value.Value);
            value.Value = 2;
            Assert.AreEqual(1, value.Value);
            signals.Update(DefaultTiming);
            Assert.AreEqual(2, value.Value);
        }
        
        // Setter to same value doesnt trigger changes
        
        [Test]
        public void ComputedWorks()
        {
            var signals = new SignalManager();
            var value = signals.CreateSignal(DefaultTiming, 1);
            var computed = signals.Computed(() => value.Value * 2);
            signals.Update(DefaultTiming);
            Assert.AreEqual(2, computed.Value);
            value.Value = 2;
            signals.Update(DefaultTiming);
            Assert.AreEqual(4, computed.Value);
        }

        [Test]
        public void EventsDontMultiply()
        {
            
        }

        [Test]
        public void SignalsDispose()
        {
            
        }

        [Test]
        public void CyclicDependenciesDetected()
        {
            
        }
    }
}
