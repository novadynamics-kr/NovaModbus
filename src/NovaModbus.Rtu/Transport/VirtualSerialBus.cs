namespace NovaModbus.Rtu.Transport;

/// <summary>Named registry handing off virtual endpoint pairs (demo/test use).</summary>
public sealed class VirtualSerialBus
{
    private readonly object _sync = new();
    private readonly Dictionary<string, ISerialTransport> _published = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Shared process-wide bus.</summary>
    public static VirtualSerialBus Shared { get; } = new();

    /// <summary>Publish one end under a name (slave/demo side). Overwrites silently.</summary>
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
    public bool Unpublish(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        lock (_sync)
        {
            return _published.Remove(endpoint);
        }
    }

    /// <summary>Currently published endpoint names.</summary>
    public string[] ListEndpoints()
    {
        lock (_sync)
        {
            return [.. _published.Keys];
        }
    }
}
