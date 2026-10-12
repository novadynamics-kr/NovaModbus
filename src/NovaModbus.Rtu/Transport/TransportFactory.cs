namespace NovaModbus.Rtu.Transport;

/// <summary>Opens serial or virtual transports behind one call site.</summary>
public static class TransportFactory
{
    /// <summary>Open a real serial port.</summary>
    /// <param name="settings">Serial port settings to open with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open serial transport.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="settings"/> are invalid.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    public static async Task<ISerialTransport> OpenSerialAsync(SerialSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        SystemPortsSerialTransport transport = new();
        await transport.OpenAsync(settings, cancellationToken).ConfigureAwait(false);
        return transport;
    }

    /// <summary>Take a published virtual endpoint (exclusive handoff).</summary>
    /// <param name="endpoint">Published endpoint name to connect to.</param>
    /// <param name="bus">Virtual bus to use, or null for the shared bus.</param>
    /// <returns>The published virtual transport endpoint.</returns>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No endpoint is published under <paramref name="endpoint"/>.</exception>
    public static ISerialTransport OpenVirtual(string endpoint, VirtualSerialBus? bus = null) =>
        (bus ?? VirtualSerialBus.Shared).Connect(endpoint);
}
