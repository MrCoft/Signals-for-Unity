using System;

namespace Coft.Signals
{
    [Serializable]
    public class FloatSignal : Signal<float>
    {
        public FloatSignal(SignalContext context, int timing, float value) : base(context, timing, value)
        {
        }

        // public FloatSignal()
        // {
        // }
        //
        // public FloatSignal(float initialValue)
        //     : base(initialValue)
        // {
        // }
    }
}
