namespace NovaModbus.Rtu.Transport;

/// <summary>Named registry handing off virtual endpoint pairs (demo/test use).</summary>
public sealed class VirtualSerialBus
{
    private readonly object _sync = new();
    private readonly Dictionary<string, ISerialTransport> _published = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Shared process-wide bus.</summary>
    public static VirtualSerialBus Shared { get; } = new();

    /// <summary>Publish one end under a name (slave/demo side). Overwrites silently.</summary>
    /// <param name="endpoint">Endpoint name to publish under.</param>
    /// <param name="transport">Transport endpoint to publish.</param>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="transport"/> is null.</exception>
    public void Publish(string endpoint, ISerialTransport transport)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(transport);
        lock (_sync)
        {
            _published[endpoint] = transport;
        }
    }

    /// <summary>Take the published end (exclusive handoff). Throws when missing.</summary>
    /// <param name="endpoint">Endpoint name to connect to.</param>
    /// <returns>The published transport endpoint.</returns>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No endpoint is published under <paramref name="endpoint"/>.</exception>
    public ISerialTransport Connect(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        lock (_sync)
        {
            if (!_published.Remove(endpoint, out ISerialTransport? transport) || transport is null)
            {
                throw new InvalidOperationException($"Virtual endpoint '{endpoint}' is not published.");
            }

            return transport;
        }
    }

    /// <summary>Remove a published end. Returns true when removed.</summary>
    /// <param name="endpoint">Endpoint name to remove.</param>
    /// <returns>True when the endpoint was published and removed; otherwise false.</returns>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is null or whitespace.</exception>
    public bool Unpublish(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        lock (_sync)
        {
            return _published.Remove(endpoint);
        }
    }

    /// <summary>Currently published endpoint names.</summary>
    /// <returns>Names of the currently published endpoints.</returns>
    public string[] ListEndpoints()
    {
        lock (_sync)
        {
            return [.. _published.Keys];
        }
    }
}
