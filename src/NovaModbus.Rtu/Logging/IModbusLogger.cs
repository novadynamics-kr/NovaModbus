namespace NovaModbus.Rtu.Logging;

/// <summary>Minimal logger abstraction (no external logger dependency).</summary>
public interface IModbusLogger
{
    /// <summary>Log TX frame.</summary>
    /// <param name="frame">Transmitted frame bytes.</param>
    void LogTx(byte[] frame);

    /// <summary>Log RX frame.</summary>
    /// <param name="frame">Received frame bytes.</param>
    void LogRx(byte[] frame);

    /// <summary>Log informational message.</summary>
    /// <param name="message">Informational message to log.</param>
    void LogInfo(string message);
}

/// <summary>Null logger (default).</summary>
public sealed class NullModbusLogger : IModbusLogger
{
    /// <summary>Shared instance.</summary>
    public static readonly NullModbusLogger Instance = new();

    private NullModbusLogger()
    {
    }

    /// <inheritdoc />
    public void LogTx(byte[] frame)
    {
    }

    /// <inheritdoc />
    public void LogRx(byte[] frame)
    {
    }

    /// <inheritdoc />
    public void LogInfo(string message)
    {
    }
}
