namespace NovaModbus.Rtu.Transport;

/// <summary>Abstraction over the serial line. Enables Fake transports in tests.</summary>
public interface ISerialTransport : IAsyncDisposable
{
    /// <summary>Whether the port is open.</summary>
    bool IsOpen { get; }

    /// <summary>Open with the given settings.</summary>
    /// <param name="settings">Serial port settings to open with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    Task OpenAsync(SerialSettings settings, CancellationToken cancellationToken = default);

    /// <summary>Write bytes to the line.</summary>
    /// <param name="buffer">Bytes to write to the line.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">The transport is closed or not paired.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>Read up to <paramref name="destination"/> length. Returns bytes read.</summary>
    /// <param name="destination">Buffer receiving the bytes read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of bytes read, or 0 at end of stream.</returns>
    /// <exception cref="InvalidOperationException">The transport is closed.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    Task<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default);

    /// <summary>Discard pending RX bytes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    Task DiscardInBufferAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Raised when the line reports parity/framing/overrun errors.
    /// Transports without error reporting never raise it.
    /// </summary>
    event EventHandler<SerialLineError>? LineError;
}
