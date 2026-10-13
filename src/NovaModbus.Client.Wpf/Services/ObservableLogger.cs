using System.Collections.ObjectModel;
using System.Windows.Threading;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Logging;

namespace NovaModbus.Client.Wpf.Services;

/// <summary><see cref="IModbusLogger"/> binding to a capped line list (UI thread marshalled).</summary>
public sealed class ObservableLogger : IModbusLogger
{
    private readonly Dispatcher _dispatcher;

    /// <summary>Initializes with the current dispatcher (construct on the UI thread).</summary>
    public ObservableLogger()
        : this(Dispatcher.CurrentDispatcher)
    {
    }

    /// <summary>Initializes with an explicit dispatcher.</summary>
    public ObservableLogger(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
    }

    /// <summary>Log lines (oldest first, capped).</summary>
    public ObservableCollection<string> Lines { get; } = [];

    /// <summary>Max retained lines.</summary>
    public int Capacity { get; set; } = 500;

    /// <inheritdoc />
    public void LogTx(byte[] frame) => Add($"TX {Hex.Format(frame)}");

    /// <inheritdoc />
    public void LogRx(byte[] frame) => Add($"RX {Hex.Format(frame)}");

    /// <inheritdoc />
    public void LogInfo(string message) => Add($"INFO {message}");

    private void Add(string body)
    {
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.Invoke(() => Add(body));
            return;
        }

        Lines.Add($"[{DateTime.Now:HH:mm:ss.fff}] {body}");
        while (Lines.Count > Capacity)
        {
            Lines.RemoveAt(0);
        }
    }
}
