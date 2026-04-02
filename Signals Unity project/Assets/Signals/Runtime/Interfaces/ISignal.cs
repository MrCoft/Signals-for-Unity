namespace Coft.Signals
{
    public interface ISignal<out T>
    {
        T Value { get; }
        T Peek();
    }
}
