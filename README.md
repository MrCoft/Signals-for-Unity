<br/>

<h1 align='center'>Signals for Unity</h1>

<p align='center'><b>Reactivity for .NET — observable values and auto-tracked side-effects</b></p>

<p align='center'>
  <a href="https://www.nuget.org/packages/Coft.Signals">NuGet</a> ·
  <a href="https://github.com/MrCoft/Signals-for-Unity">GitHub</a>
</p>

<br/>

## Overview

Signals for Unity brings a signals-based reactivity model to C# and Unity. Inspired by reactive primitives found in SolidJS and Preact Signals, it lets you declare observable values (`Signal`), derived values (`Computed`), and side-effects (`Effect`) that automatically track their own dependencies — no manual subscriptions required.

Updates are batched and resolved in topological order per a **timing** integer, making it straightforward to wire signals into Unity's different update loops (e.g. `Update`, `FixedUpdate`, `LateUpdate`).

## Core concepts

| Type | Description |
|------|-------------|
| `Signal<T>` | A writable observable value. Reading `.Value` inside a `Computed` or `Effect` registers a dependency automatically. |
| `Computed<T>` | A read-only derived value. Re-evaluated lazily when any dependency changes. |
| `Effect` | A side-effect action that re-runs whenever its signal dependencies change. |
| `SignalContext` | Owns a set of signals and drives updates. Call `Update(timing)` each frame to flush dirty signals. |

## Installation

### Unity Package Manager

Add via git URL in the Package Manager:
```
https://github.com/MrCoft/Signals-for-Unity.git?path=Signals Unity project/Assets/Signals
```

### NuGet

```shell
dotnet add package Coft.Signals
```

## Usage

### Creating a context

```csharp
var ctx = new SignalContext();
const int updateTiming = 0; // group signals by Unity loop
```

### Signal — writable value

```csharp
var health = ctx.Signal(updateTiming, 100);

health.Value = 80; // marks signal dirty
```

### Computed — derived value

```csharp
var isDead = ctx.Computed(updateTiming, () => health.Value <= 0);
// isDead.Value is recalculated automatically when health changes
```

### Effect — side-effect

```csharp
var effect = ctx.Effect(updateTiming, () =>
{
    Debug.Log($"Health changed: {health.Value}");
});
// Runs once immediately, then re-runs whenever health.Value changes
```

### Flushing updates

Call `Update` once per frame (or per fixed update, etc.) to propagate changes:

```csharp
void Update()
{
    ctx.Update(updateTiming);
}
```

`Update` resolves signals → computeds → effects in topological order. Effects whose dependencies haven't settled yet are deferred to the next pass automatically.

### Disposing

`Computed` and `Effect` implement `IDisposable`. Dispose them when the owning object is destroyed to unsubscribe from all dependencies:

```csharp
void OnDestroy()
{
    isDead.Dispose();
    effect.Dispose();
}
```

## Timing

The `timing` integer lets you bucket signals by update phase:

```csharp
const int FixedTiming = 0;
const int UpdateTiming = 1;
const int LateTiming = 2;

void FixedUpdate() => ctx.Update(FixedTiming);
void Update()      => ctx.Update(UpdateTiming);
void LateUpdate()  => ctx.Update(LateTiming);
```

A signal and its dependents are only processed during the timing they were created with.

## Requirements

- Unity 2019.1+
- .NET Standard 2.0 (NuGet target)

## License

[MIT](Signals%20Unity%20project/Assets/Signals/LICENSE.md)
