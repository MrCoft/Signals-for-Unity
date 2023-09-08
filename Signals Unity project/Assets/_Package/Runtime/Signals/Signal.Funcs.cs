using System;

namespace Coft.Signals
{
    public static class Signal
    {
        public static void Effect(Action action)
        {
            _ = new Effect(action);
        }
    }
}