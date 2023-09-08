using System;
using UniRx;

namespace Coft.Signals
{
    [Serializable]
    public class FloatSignal : Signal<float>
    {
        public FloatSignal()
        {
        }

        public FloatSignal(float initialValue)
            : base(initialValue)
        {
        }

        public FloatSignal(ReactiveProperty<float> property)
            : base(property)
        {
        }
    }
}