using System.Collections.ObjectModel;
using NovaModbus.Client.Services;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Commands;

namespace NovaModbus.Client.ViewModels;

/// <summary>Registered watch list (entries + results). Execution lands in stage 2.</summary>
public sealed class WatchViewModel : ViewModelBase, IDisposable
{
    private readonly Func<ModbusRtuClient?> _clientProvider;
    private bool _disposed;
    private string _slaveIdText = "1";
    private WatchArea _area = WatchArea.HoldingRegisters;
    private string _startText = "0";
    private string _countText = "1";
    private WatchEntry? _selectedEntry;

    /// <summary>Initializes with a live client provider (null when disconnected).</summary>
    public WatchViewModel(Func<ModbusRtuClient?> clientProvider)
    {
        ArgumentNullException.ThrowIfNull(clientProvider);
        _clientProvider = clientProvider;
        AddCommand = new RelayCommand(AddEntry);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => SelectedEntry is not null);
        ReadAllCommand = new AsyncRelayCommand(ReadAllAsync, () => !IsBusy, OnError);
    }

    /// <summary>Supported areas.</summary>
    public IReadOnlyList<WatchArea> Areas { get; } =
    [
        WatchArea.HoldingRegisters,
        WatchArea.InputRegisters,
        WatchArea.Coils,
        WatchArea.DiscreteInputs,
    ];

    /// <summary>Registered entries.</summary>
    public ObservableCollection<WatchEntry> Entries { get; } = [];

    /// <summary>Result rows (1:1 with entries).</summary>
    public ObservableCollection<WatchResult> Results { get; } = [];

    /// <summary>New-entry slave ID text.</summary>
    public string SlaveIdText
    {
        get => _slaveIdText;
        set => SetProperty(ref _slaveIdText, value);
    }

    /// <summary>New-entry area.</summary>
    public WatchArea Area
    {
        get => _area;
        set => SetProperty(ref _area, value);
    }

    /// <summary>New-entry start address text.</summary>
    public string StartText
    {
        get => _startText;
        set => SetProperty(ref _startText, value);
    }

    /// <summary>New-entry count text.</summary>
    public string CountText
    {
        get => _countText;
        set => SetProperty(ref _countText, value);
    }

    /// <summary>Selected entry (enables Remove).</summary>
    public WatchEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                RemoveSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Add the configured entry.</summary>
    public RelayCommand AddCommand { get; }

    /// <summary>Remove the selected entry and its result row.</summary>
    public RelayCommand RemoveSelectedCommand { get; }

    /// <summary>Read all entries once.</summary>
    public AsyncRelayCommand ReadAllCommand { get; }

    /// <summary>Auto-refresh on interval.</summary>
    public bool AutoRefresh
    {
        get => _autoRefresh;
        set
        {
            if (SetProperty(ref _autoRefresh, value))
            {
                if (value)
                {
                    StartAuto();
                }
                else
                {
                    StopAuto();
                }
            }
        }
    }

    private bool _autoRefresh;
    private CancellationTokenSource? _autoCts;
    private TimeSpan _autoInterval = TimeSpan.FromSeconds(1);

    /// <summary>Auto-refresh interval text in milliseconds.</summary>
    public string AutoIntervalMsText
    {
        get => _autoIntervalMsText;
        set => SetProperty(ref _autoIntervalMsText, value);
    }

    private string _autoIntervalMsText = "1000";

    /// <summary>Read-all in progress.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ReadAllCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private bool _isBusy;

    /// <summary>Last add/remove message.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    private string _statusText = string.Empty;

    private void AddEntry()
    {
        try
        {
            WatchEntry entry = ParseEntry();
            Entries.Add(entry);
            Results.Add(new WatchResult(entry));
            StatusText = string.Empty;
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private void RemoveSelected()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        WatchResult? row = Results.FirstOrDefault(r => r.Entry == SelectedEntry);
        if (row is not null)
        {
            Results.Remove(row);
        }

        Entries.Remove(SelectedEntry);
        SelectedEntry = null;
    }

    private async Task ReadAllAsync(CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            ModbusRtuClient? client = _clientProvider();
            if (client is null)
            {
                MarkAll("Not connected.", "Error");
                return;
            }

            foreach (WatchResult row in Results.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ReadOneAsync(client, row, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static async Task ReadOneAsync(ModbusRtuClient client, WatchResult row, CancellationToken ct)
    {
        WatchEntry entry = row.Entry;
        try
        {
            row.ValuesText = entry.Area switch
            {
                WatchArea.HoldingRegisters => ValueFormatter.FormatRegisters(
                    await client.ReadHoldingRegistersAsync(entry.SlaveId, entry.Start, entry.Count, ct).ConfigureAwait(false)),
                WatchArea.InputRegisters => ValueFormatter.FormatRegisters(
                    await client.ReadInputRegistersAsync(entry.SlaveId, entry.Start, entry.Count, ct).ConfigureAwait(false)),
                WatchArea.Coils => ValueFormatter.FormatBits(
                    await client.ReadCoilsAsync(entry.SlaveId, entry.Start, entry.Count, ct).ConfigureAwait(false)),
                WatchArea.DiscreteInputs => ValueFormatter.FormatBits(
                    await client.ReadDiscreteInputsAsync(entry.SlaveId, entry.Start, entry.Count, ct).ConfigureAwait(false)),
                _ => throw new InvalidOperationException($"Unknown area {entry.Area}."),
            };
            row.Timestamp = DateTime.Now.ToString("HH:mm:ss.fff", null);
            row.Status = "OK";
        }
        catch (Exception ex)
        {
            row.Status = $"Error: {ex.Message}";
        }
    }

    private void MarkAll(string values, string status)
    {
        foreach (WatchResult row in Results)
        {
            row.ValuesText = values;
            row.Status = status;
        }
    }

    private void OnError(Exception ex) => StatusText = $"Error: {ex.Message}";

    private void StartAuto()
    {
        if (!int.TryParse(AutoIntervalMsText, out int ms) || ms <= 0)
        {
            StatusText = $"Invalid interval: '{AutoIntervalMsText}'.";
            _autoRefresh = false;
            NotifyPropertyChanged(nameof(AutoRefresh));
            return;
        }

        StopAuto();
        _autoInterval = TimeSpan.FromMilliseconds(ms);
        _autoCts = new CancellationTokenSource();
        _ = AutoLoopAsync(_autoCts.Token);
    }

    /// <summary>Stop auto-refresh (safe when idle).</summary>
    public void StopAuto()
    {
        try
        {
            _autoCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already torn down.
        }

        _autoCts?.Dispose();
        _autoCts = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAuto();
        GC.SuppressFinalize(this);
    }

    private async Task AutoLoopAsync(CancellationToken ct)
    {
        try
        {
            using PeriodicTimer timer = new(_autoInterval);
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                await ReadAllAsync(ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
        catch (Exception ex)
        {
            StatusText = $"Auto-refresh stopped: {ex.Message}";
            _autoRefresh = false;
            NotifyPropertyChanged(nameof(AutoRefresh));
        }
    }

    private WatchEntry ParseEntry()
    {
        if (!byte.TryParse(SlaveIdText, out byte slaveId) || slaveId is 0 or > 247)
        {
            throw new ArgumentException($"Invalid slave ID: '{SlaveIdText}' (1-247).");
        }

        if (!ushort.TryParse(StartText, out ushort start))
        {
            throw new ArgumentException($"Invalid start address: '{StartText}'.");
        }

        if (!ushort.TryParse(CountText, out ushort count) || count == 0)
        {
            throw new ArgumentException($"Invalid count: '{CountText}'.");
        }

        int max = Area is WatchArea.Coils or WatchArea.DiscreteInputs
            ? ModbusLimits.MaxReadBits
            : ModbusLimits.MaxReadRegisters;
        if (count > max)
        {
            throw new ArgumentException($"Count exceeds {Area} limit ({max}).");
        }

        return new WatchEntry(slaveId, Area, start, count);
    }
}
