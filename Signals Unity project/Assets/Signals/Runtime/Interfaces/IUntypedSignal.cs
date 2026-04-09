namespace Coft.Signals
{
    public interface IUntypedSignal
    {
        int Level { get; }
        bool IsReady { get; set; }
        bool HasChangedThisPass { get; }
        
        void Update();
        WeakHashSet<IUntypedComputed> ComputedSubscribers { get; }
        WeakHashSet<Effect> EffectSubscribers { get; }
    }
}
