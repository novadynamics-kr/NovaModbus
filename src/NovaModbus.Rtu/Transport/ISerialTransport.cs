namespace NovaModbus.Rtu.Transport;

/// <summary>Abstraction over the serial line. Enables Fake transports in tests.</summary>
public interface ISerialTransport : IAsyncDisposable
{
    /// <summary>Whether the port is open.</summary>
    bool IsOpen { get; }

    /// <summary>Open with the given settings.</summary>
    Task OpenAsync(SerialSettings settings, CancellationToken cancellationToken = default);

    /// <summary>Write bytes to the line.</summary>
    Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>Read up to <paramref name="destination"/> length. Returns bytes read.</summary>
    Task<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default);

    /// <summary>Discard pending RX bytes.</summary>
    Task DiscardInBufferAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Raised when the line reports parity/framing/overrun errors.
    /// Transports without error reporting never raise it.
    /// </summary>
    event EventHandler<SerialLineError>? LineError;
}
