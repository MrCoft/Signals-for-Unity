namespace Coft.Signals
{
    public interface IReadOnlyReactiveValue<T>
    {
        public T Value { get; }
    }
}