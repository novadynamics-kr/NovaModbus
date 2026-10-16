using System.ComponentModel;

namespace NovaModbus.Client.ViewModels;

/// <summary>Periodic re-execution of the Read/Write tab request.</summary>
/// <remarks>UI-affine (no ConfigureAwait(false)): History is bound.</remarks>
public sealed class PollingRunner : IDisposable
{
    private readonly ReadWriteRunner _target;
    private CancellationTokenSource? _cts;
    private bool _isPolling;
    private bool _disposed;

    /// <summary>Initializes polling for the given runner.</summary>
    public PollingRunner(ReadWriteRunner target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
    }

    /// <summary>Interval text in milliseconds.</summary>
    public string IntervalMsText { get; set; } = "1000";

    /// <summary>Whether the loop runs.</summary>
    public bool IsPolling
    {
        get => _isPolling;
        private set
        {
            if (_isPolling != value)
            {
                _isPolling = value;
                IsPollingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Raised when <see cref="IsPolling"/> changes.</summary>
    public event EventHandler? IsPollingChanged;

    /// <summary>Decoded result history (newest last, capped).</summary>
    public BindingList<string> History { get; } = [];

    /// <summary>Start the loop (no-op when already running).</summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
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

    /// <summary>Stop the loop.</summary>
    public void Stop()
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
}
