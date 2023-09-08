namespace Coft.Signals
{
    public interface IReactiveValue<T> : IReadOnlyReactiveValue<T>
    {
        public T Value { get; set; }
    }
}
