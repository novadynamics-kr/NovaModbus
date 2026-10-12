namespace NovaModbus.Rtu.Transport;

/// <summary>In-memory <see cref="ISerialTransport"/> endpoint (hardware-free).</summary>
public sealed class VirtualTransport : ISerialTransport
{
    private readonly object _sync = new();
    private readonly Queue<byte> _inbound = new();
    private readonly SemaphoreSlim _signal = new(0);
    private VirtualTransport? _peer;
    private bool _isOpen = true;
    private bool _disposed;

    private VirtualTransport()
    {
    }

    /// <summary>Creates two linked endpoints (writes on one arrive on the other).</summary>
    /// <returns>A pair of linked virtual transport endpoints.</returns>
    public static (VirtualTransport Left, VirtualTransport Right) CreatePair()
    {
        VirtualTransport left = new();
        VirtualTransport right = new();
        left._peer = right;
        right._peer = left;
        return (left, right);
    }

    /// <inheritdoc />
    public bool IsOpen => _isOpen;

    /// <inheritdoc />
    /// <remarks>Never raised by line traffic (error-free by construction).</remarks>
    public event EventHandler<SerialLineError>? LineError;

    /// <summary>Raises <see cref="LineError"/> (error-injection hook for simulators and tests).</summary>
    /// <param name="error">Serial line error to report to subscribers.</param>
    public void SimulateLineError(SerialLineError error) => LineError?.Invoke(this, error);

    /// <inheritdoc />
    public Task OpenAsync(SerialSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();
        _isOpen = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        VirtualTransport? peer = _peer ?? throw new InvalidOperationException("Endpoint is not paired.");
        if (!_isOpen)
        {
            throw new InvalidOperationException("Transport is closed.");
        }

        lock (peer._sync)
        {
            foreach (byte b in buffer.Span)
            {
                peer._inbound.Enqueue(b);
            }
        }

        peer._signal.Release(buffer.Length);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        if (!_isOpen)
        {
            throw new InvalidOperationException("Transport is closed.");
        }

        while (true)
        {
            lock (_sync)
            {
                if (_inbound.Count > 0)
                {
                    int count = Math.Min(_inbound.Count, destination.Length);
                    for (int i = 0; i < count; i++)
                    {
                        destination.Span[i] = _inbound.Dequeue();
                    }

                    return count;
                }
            }

            await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!_isOpen)
            {
                throw new InvalidOperationException("Transport was closed while reading.");
            }
        }
    }

    /// <inheritdoc />
    public Task DiscardInBufferAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            _inbound.Clear();
            while (_signal.CurrentCount > 0)
            {
                _signal.Wait(0, cancellationToken);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        _isOpen = false;
        _signal.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
