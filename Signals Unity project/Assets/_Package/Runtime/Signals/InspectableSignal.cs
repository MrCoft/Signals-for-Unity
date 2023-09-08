using System;
using UniRx;

namespace Coft.Signals
{
    [Serializable]
    public class FloatSignal2 : Signal2<float>
    {
        public FloatSignal2()
        {
        }

        public FloatSignal2(float initialValue)
            : base(initialValue)
        {
        }

        public FloatSignal2(ReactiveProperty<float> property)
            : base(property)
        {
        }
    }
}