using System.ComponentModel;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Logging;

namespace NovaModbus.Slave.Services;

/// <summary><see cref="IModbusLogger"/> binding to a capped line list (marshalled via a UI control).</summary>
public sealed class WinFormsLogger : IModbusLogger
{
    private readonly Control _sync;

    /// <summary>Initializes with a UI-thread control used for marshalling.</summary>
    public WinFormsLogger(Control sync)
    {
        ArgumentNullException.ThrowIfNull(sync);
        _sync = sync;
    }

    /// <summary>Log lines (oldest first, capped). Bind to a ListBox via DataSource.</summary>
    public BindingList<string> Lines { get; } = [];

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
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {body}";
        if (_sync.InvokeRequired)
        {
            _sync.Invoke(() => Append(line));
            return;
        }

        Append(line);
    }

    private void Append(string line)
    {
        Lines.Add(line);
        while (Lines.Count > Capacity)
        {
            Lines.RemoveAt(0);
        }
    }
}
