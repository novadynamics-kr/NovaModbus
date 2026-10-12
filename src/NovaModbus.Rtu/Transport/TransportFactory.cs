namespace NovaModbus.Rtu.Transport;

/// <summary>Opens serial or virtual transports behind one call site.</summary>
public static class TransportFactory
{
    /// <summary>Open a real serial port.</summary>
    public static async Task<ISerialTransport> OpenSerialAsync(SerialSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        SystemPortsSerialTransport transport = new();
        await transport.OpenAsync(settings, cancellationToken).ConfigureAwait(false);
        return transport;
    }

    /// <summary>Take a published virtual endpoint (exclusive handoff).</summary>
    public static ISerialTransport OpenVirtual(string endpoint, VirtualSerialBus? bus = null) =>
        (bus ?? VirtualSerialBus.Shared).Connect(endpoint);
}
