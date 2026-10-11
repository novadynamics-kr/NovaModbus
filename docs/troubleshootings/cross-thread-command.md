# Cross-thread CanExecuteChanged crash (2026-10-11)

## Symptom

`InvalidOperationException` — "다른 스레드가 이 개체를 소유하고 있어..." at
`AsyncRelayCommand.RaiseCanExecuteChanged`, thrown from a threadpool thread
during normal Client use (connect → execute → poll).

## Cause

`Execute` awaits Rtu calls with `ConfigureAwait(false)`, so the `finally`
block runs on the threadpool. Raising `CanExecuteChanged` there makes WPF
touch UI-thread-owned Buttons from the wrong thread. The WPF binding engine
marshals `INotifyPropertyChanged` but NOT the `ICommand.CanExecuteChanged`
→ `CanExecute` path. Same family: background `History.Add`
(`ObservableCollection` never marshals).

## Fix

Commands capture `SynchronizationContext.Current` at construction (UI thread)
and `Post` both `CanExecuteChanged` and the error callback. `ObservableLogger`
already marshals via `Dispatcher`. Rule: INPC writes are free from any thread;
`ObservableCollection` and command events must reach the UI thread explicitly.

## Never again

Any new `ICommand` must marshal through the captured context, covered by
`CommandTests` with a recording `SynchronizationContext`.
