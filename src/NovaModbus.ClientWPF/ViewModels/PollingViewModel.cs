using System.Collections.ObjectModel;
using NovaModbus.Rtu.Commands;

namespace NovaModbus.ClientWPF.ViewModels;

/// <summary>Periodic re-execution of the Read/Write tab request.</summary>
public sealed class PollingViewModel : ViewModelBase, IDisposable
{
    private readonly ReadWriteViewModel _target;
    private CancellationTokenSource? _cts;
    private string _intervalMsText = "1000";
    private bool _isPolling;
    private bool _disposed;

    /// <summary>Initializes polling for the given tab.</summary>
    public PollingViewModel(ReadWriteViewModel target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsPolling, OnError);
        StopCommand = new RelayCommand(Stop, () => IsPolling);
    }

    /// <summary>Interval text in milliseconds.</summary>
    public string IntervalMsText
    {
        get => _intervalMsText;
        set => SetProperty(ref _intervalMsText, value);
    }

    /// <summary>Whether the loop runs.</summary>
    public bool IsPolling
    {
        get => _isPolling;
        private set
        {
            if (SetProperty(ref _isPolling, value))
            {
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Decoded result history (newest last, capped).</summary>
    public ObservableCollection<string> History { get; } = [];

    /// <summary>Start the loop.</summary>
    public AsyncRelayCommand StartCommand { get; }

    /// <summary>Stop the loop.</summary>
    public RelayCommand StopCommand { get; }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
            // Intentionally UI-affine (no ConfigureAwait(false)): History is bound.
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

    private void OnError(Exception ex) => History.Add($"Error: {ex.Message}");

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
}
