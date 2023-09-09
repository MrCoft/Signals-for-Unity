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
            var value = signals.Signal(DefaultTiming, 1);
            Assert.AreEqual(1, value.Value);
            value.Value = 2;
            Assert.AreEqual(1, value.Value);
            signals.Update(DefaultTiming);
            Assert.AreEqual(2, value.Value);
        }

        [Test]
        public void SameValueWriteIsIgnored()
        {
            var signals = new SignalManager();
            var value = signals.Signal(DefaultTiming, 1);
            var x = 0;
            signals.Effect(DefaultTiming, () => x = value.Value);
            x = 0;
            value.Value = 1;
            signals.Update(DefaultTiming);
            Assert.AreEqual(0, x);
        }
        
        
        
        
        
        [Test]
        public void EventsDontMultiply()
        {
            
        }

        [Test]
        public void SignalsDispose()
        {
            
        }
    }
}
