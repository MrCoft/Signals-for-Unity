# Disposal Strategy

## The Problem

In normal C#, when you stop referencing an object, the garbage collector cleans it up. But in this library,
signals, computeds, and effects register themselves in the `SignalContext` — in dirty sets, subscriber sets,
and dependency sets. These references root them, so even when user code drops all references, GC will never
collect them. They keep running on every update cycle forever.

This is especially problematic because it's a very common pattern for computeds and effects to create other
signals, computeds, and effects as part of their evaluation. When a computed re-evaluates, the old inner
primitives should die — but they don't, because they're rooted in the context.

## What We Implemented

### Effect Cleanup

An effect callback can optionally return an `IDisposable`. The effect stores it and disposes it:

- Before each re-run
- When the effect itself is disposed

```csharp
// Action overload — works as before
context.Effect(timing, () => { /* ... */ });

// Func<IDisposable> overload — cleanup on re-run
context.Effect(timing, () =>
{
    var subscription = SomethingExpensive();
    return subscription; // disposed before next run
});
```

This is an explicit API — the consumer says "here's what to clean up." The library isn't guessing about
ownership; the consumer is telling it directly.

## What We Considered But Did Not Implement

### Computed Auto-Disposing Its Previous Value

The idea: when a `Computed<T>` replaces `_committedValue` with a new value in `Update()`, if the old value
implements `IDisposable`, dispose it automatically.

```csharp
// In Computed.Update():
if (HasChangedThisPass)
{
    if (_committedValue is IDisposable oldDisposable)
        oldDisposable.Dispose();
    _committedValue = _pendingValue;
}
```

**Why it seems right:** The computed produced the value, the computed is replacing it. That's ownership.
Computeds are assumed to be functional — they derive values, they don't share mutable state.

**Why we didn't do it:** See "Why We're Not Doing Any Of This" below.

### Computed Auto-Disposing Items in Collections

The idea: when a computed returns a collection (e.g. `List<object>`), and on re-evaluation returns a new
collection, diff the old and new collections and dispose items that are no longer present.

This would solve the common "generated props" pattern where a computed builds a list of UI props objects,
each containing inner signals. On re-evaluation, all old props (and their inner signals) would be disposed.

**Why it seems right:** The computed created these items as part of its evaluation. They're derived values.
Nobody else should be holding references to them.

**Why we didn't do it:** See below.

### ListChangeTracker Auto-Disposing Removed Items

The idea: `ListChangeTracker` already knows exactly which items were removed (it has a `Removed` list).
If removed items implement `IDisposable`, dispose them.

This would be elegant because:
- `ComputedList` uses `ListChangeTracker` internally, so it gets disposal for free
- Any other consumer of `ListChangeTracker` also benefits
- It's the "last point that knows" about items before they disappear

**The problem:** ListChangeTracker observes, it doesn't own. It tracks changes on a source that could be
anything — a `Signal`, a `Computed`, a plain list. If the source is a `Computed` that also auto-disposes,
you get double disposal. If the source is a `Signal` where items are shared across multiple consumers,
you get use-after-dispose.

### ComputedList Auto-Disposing Mapped Values

The idea: `ComputedList` maintains a `mapping` dictionary from `TIn` to `TOut`. When an item is removed,
the mapped `TOut` value is dropped. If it's `IDisposable`, dispose it.

**Why it seems right:** ComputedList created the `TOut` via `map(item)`. It owns them. It's the one
dropping them.

**Why it interacts badly:** If the source is a `Computed` that also auto-disposes, the `TIn` items get
disposed by the computed and the `TOut` items get disposed by ComputedList. Two different disposal systems
acting on related objects. If the `TOut` holds a reference to `TIn`, it might be accessing a disposed
object.

### Signal Auto-Disposing Previous Values

The idea: when a signal is updated and the old value is replaced, dispose the old value.

**Immediately rejected:** Signals are imperative and mutable. The same value can be assigned to multiple
signals. A value can be read back out before the signal updates. There's no ownership assumption that makes
sense for signals. Same reasoning applies to `SignalObject` and `SignalList`.

### Ownership Tracking Between Primitives

The idea: since we already do implicit dependency tracking (we know which signals a computed reads), we
could also track which primitives *created* which other primitives. Then disposing a parent would dispose
its children.

**Why we didn't do it:** This is essentially a factory/lifetime management pattern. It's out of scope for
this library — collecting and managing disposables is the consumer's responsibility. It would also add
complexity and overhead to every primitive creation.

## Why We're Not Doing Any Of This (Except Effect Cleanup)

### The Core Issue: We Can't Make Assumptions

Unlike React, where the contract is strict (pure render functions, immutable props, framework controls the
lifecycle), this library is a low-level reactive primitive toolkit. The consumer decides the patterns:

- **Is the value shared?** A computed might return an object that something else also references.
  Auto-disposing it would dispose it out from under the other reference holder.

- **Who owns it?** If a `Computed` produces a list, and a `ListChangeTracker` consumes it, which one
  should dispose removed items? Doing both causes double-dispose. Doing one requires knowing what the
  other one is doing.

- **Is it mutable or functional?** Computeds are *assumed* to be functional, but nothing enforces it.
  Signals are explicitly mutable. The same value types flow through both.

- **What's underneath?** A `ListChangeTracker` might be tracking a `Computed` (which might auto-dispose)
  or a `SignalList` (which won't). Its behavior would need to depend on what produced its source, which
  it can't and shouldn't know.

### The Inconsistency Problem

If we auto-dispose in *some* primitives but not others, the consumer has to remember a complex set of
rules:

- "Computed disposes its previous value, but Signal doesn't"
- "ListChangeTracker disposes removed items if the source is a Signal, but not if it's a Computed"
- "ComputedList disposes its mapped values, but not its input values"

One wrong assumption about which layer is disposing leads to either a leak or a double-dispose.
**The inconsistency is worse than not having the feature at all.**

### The Decision

The library provides `IDisposable` on all primitives. The consumer handles disposal. One simple rule,
no surprises, no edge cases.

The one exception — Effect returning `IDisposable` for cleanup — is not auto-disposal magic. It's an
explicit API where the consumer tells the effect what to clean up. The effect is the consumer of its own
cleanup resource.

### What This Means for Consumers

Consumers who create disposable objects inside computeds or effects should manage them explicitly:

- Cache values and dispose them when a computed or effect re-runs
- Use the Effect cleanup return value for resources tied to an effect's lifecycle
- Call `.Dispose()` on primitives when they're no longer needed

This is more verbose than auto-disposal would be, but it's predictable and correct in all cases.
