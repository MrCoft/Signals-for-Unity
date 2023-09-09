namespace Coft.Signals
{
    public interface ISignal<T> : IReadOnlySignal<T>
    {
        public T Value { get; set; }
    }
}
