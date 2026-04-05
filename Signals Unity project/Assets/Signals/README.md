# Signals for Unity

Reactive signals for Unity. Declare observable values, derived computeds, and side effects — the dependency graph is tracked automatically.

## Installation

Add via the Unity Package Manager using the git URL:

```
https://github.com/MrCoft/Signals-for-Unity.git?path=Assets/Signals
```

Requires Unity 2021.2+.

## Core concepts

Everything lives inside a `SignalContext`. Call `Update(timing)` to flush pending changes for a given timing bucket.

```csharp
var context = new SignalContext();
```

### Signal

An observable value. Writes are deferred until the next `Update`.

```csharp
var health = context.Signal(0, 100);

health.Value = 80;         // queued, not applied yet
context.Update(0);
Debug.Log(health.Value);   // 80
```

### Computed

A derived value. Re-evaluates automatically when its dependencies change.

```csharp
var isAlive = context.Computed(0, () => health.Value > 0);

context.Update(0);
Debug.Log(isAlive.Value);  // true
```

### Effect

A side effect. Re-runs automatically when its dependencies change.

```csharp
context.Effect(0, () => Debug.Log($"Health changed: {health.Value}"));
context.Update(0);  // runs the effect once immediately
```

### SignalList

An observable list. Mutations (Add, Remove, Clear, etc.) notify dependents.

```csharp
var inventory = context.List<string>(0);
context.Effect(0, () => Debug.Log($"Items: {inventory.Count}"));

context.Update(0);
inventory.Add("Sword");
context.Update(0);  // effect re-runs
```

## Timing

Every signal, computed, and effect is assigned a timing bucket (an `int`). `Update(timing)` only flushes that bucket. This lets you drive different parts of the graph from different Unity loops.

```csharp
var input  = context.Signal(0, Vector2.zero);   // timing 0 — flushed in Update
var physics = context.Signal(1, Vector3.zero);  // timing 1 — flushed in FixedUpdate

void Update()      => context.Update(0);
void FixedUpdate() => context.Update(1);
```

A computed or effect at timing 1 that reads a signal at timing 0 will update at timing 1, not timing 0.

## Dispose

`Computed<T>` and `Effect` implement `IDisposable`. Disposing removes them from the graph and stops updates.

```csharp
var effect = context.Effect(0, () => Debug.Log(health.Value));
context.Update(0);

effect.Dispose();  // no longer runs
```

## Custom comparers

Pass a custom `IEqualityComparer<T>` to suppress notifications when the value hasn't meaningfully changed.

```csharp
var position = context.Signal(0, Vector3.zero, new NearlyEqualComparer(0.01f));
```

## Error handling

A throwing computed or effect does not stop others from running. All errors in a single `Update` are collected and thrown together as one exception at the end.
