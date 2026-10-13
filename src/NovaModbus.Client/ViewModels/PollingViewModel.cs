using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NovaModbus.Client.ViewModels;

/// <summary>Periodic re-execution of the Read/Write tab request.</summary>
/// <remarks>UI-affine (no ConfigureAwait(false)): History is bound.</remarks>
public sealed partial class PollingViewModel : ObservableObject, IDisposable
{
    private readonly ReadWriteViewModel _target;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    [ObservableProperty]
    public partial string IntervalMsText { get; set; } = "1000";

    [ObservableProperty]
    public partial bool IsPolling { get; private set; }

    /// <summary>Initializes polling for the given tab.</summary>
    public PollingViewModel(ReadWriteViewModel target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsPolling);
        StopCommand = new RelayCommand(Stop, () => IsPolling);
    }

    /// <summary>Decoded result history (newest last, capped).</summary>
    public ObservableCollection<string> History { get; } = [];

    /// <summary>Start the loop.</summary>
    public IAsyncRelayCommand StartCommand { get; }

    /// <summary>Stop the loop.</summary>
    public IRelayCommand StopCommand { get; }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (IsPolling)
            {
                return;
            }

            if (!int.TryParse(IntervalMsText, out int ms) || ms <= 0)
            {
                History.Add($"Invalid interval: '{IntervalMsText}'.");
                return;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            IsPolling = true;
            try
            {
                using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(ms));
                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    string result = await _target.RunOnceAsync(_cts.Token);
                    History.Add($"[{DateTime.Now:HH:mm:ss.fff}] {SingleLine(result)}");
                    while (History.Count > 200)
                    {
                        History.RemoveAt(0);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal stop.
            }
            catch (Exception ex)
            {
                History.Add($"Polling stopped: {ex.Message}");
            }
            finally
            {
                IsPolling = false;
            }
        }
        catch (Exception ex)
        {
            History.Add($"Error: {ex.Message}");
        }
    }

    private void Stop()
    {
        try
        {
            _cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed via app shutdown.
        }

        _cts?.Dispose();
        _cts = null;
    }

    private static string SingleLine(string text) => text.Replace(Environment.NewLine, " | ", StringComparison.Ordinal);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        GC.SuppressFinalize(this);
    }

    partial void OnIsPollingChanged(bool value)
    {
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }
}
