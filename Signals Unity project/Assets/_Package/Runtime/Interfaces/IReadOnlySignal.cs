namespace Coft.Signals
{
    public interface IReadOnlySignal<T>
    {
        public T Value { get; }
    }
}