# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/) and this project adheres to [Semantic Versioning](http://semver.org/).

## [0.1.0] - 2026-03-28

### Added
- `Signal<T>` — observable value with deferred writes
- `Computed<T>` — derived value that updates automatically when dependencies change
- `Effect` — side effect that re-runs automatically when dependencies change
- `ReactiveList<T>` — observable list that triggers dependents on mutation
- `SignalContext` — owns and manages the reactive graph; call `Update(timing)` to flush changes
- Timing system — signals, computeds, and effects each declare a timing bucket; `Update(timing)` only flushes that bucket, allowing separate update loops (e.g. Update, FixedUpdate, LateUpdate)
- Cross-timing dependencies — a computed or effect at timing 3 can read a signal at timing 1 and will update at its own timing, not the signal's
- Custom equality comparers on `Signal<T>` and `Computed<T>` to control when dependents are notified
- `Dispose()` on `Computed<T>` and `Effect` to stop updates and unsubscribe from the graph
- Error resilience — a throwing computed or effect does not prevent others from running; errors are collected and thrown together at the end of `Update`
- Cycle detection — circular dependencies are detected and reported after 50 passes
