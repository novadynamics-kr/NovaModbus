using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu.Tests.Testing;

/// <summary>In-memory <see cref="ISerialTransport"/> for tests.</summary>
public sealed class FakeSerialTransport : ISerialTransport
{
    private readonly object _sync = new();
    private readonly Queue<byte> _inbound = new();
    private readonly Queue<byte> _written = new();
    private readonly SemaphoreSlim _signal = new(0);
    private FakeSerialTransport? _peer;
    private bool _isOpen = true;
    private bool _disposed;

    /// <summary>Creates two linked transports (writes on one arrive on the other).</summary>
    public static (FakeSerialTransport Left, FakeSerialTransport Right) CreatePair()
    {
        FakeSerialTransport left = new();
        FakeSerialTransport right = new();
        left._peer = right;
        right._peer = left;
        return (left, right);
    }

    /// <inheritdoc />
    public bool IsOpen => _isOpen;

    /// <inheritdoc />
    public event EventHandler<SerialLineError>? LineError;

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
        if (!_isOpen)
        {
            throw new InvalidOperationException("Transport is closed.");
        }

        if (_peer is not null)
        {
            _peer.EnqueueInbound(buffer.Span);
            return Task.CompletedTask;
        }

        lock (_sync)
        {
            foreach (byte b in buffer.Span)
            {
                _written.Enqueue(b);
            }
        }

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

    /// <summary>Scripts incoming bytes (as if received from the line).</summary>
    public void EnqueueInbound(ReadOnlySpan<byte> data)
    {
        lock (_sync)
        {
            foreach (byte b in data)
            {
                _inbound.Enqueue(b);
            }
        }

        _signal.Release(data.Length);
    }

    /// <summary>Simulates a line error (as if reported by the port).</summary>
    public void SimulateLineError(SerialLineError error) => LineError?.Invoke(this, error);

    /// <summary>Drains bytes written to this transport (single-ended use).</summary>
    public byte[] TakeWritten()
    {
        lock (_sync)
        {
            byte[] data = [.. _written];
            _written.Clear();
            return data;
        }
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
