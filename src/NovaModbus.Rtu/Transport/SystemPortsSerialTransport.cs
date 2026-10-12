using System.IO.Ports;

namespace NovaModbus.Rtu.Transport;

/// <summary><see cref="ISerialTransport"/> adapter over <see cref="SerialPort"/> (Microsoft 1st-party).</summary>
public sealed class SystemPortsSerialTransport : ISerialTransport
{
    private SerialPort? _port;

    /// <inheritdoc />
    public bool IsOpen => _port?.IsOpen == true;

    /// <inheritdoc />
    public event EventHandler<SerialLineError>? LineError;

    /// <inheritdoc />
    public Task OpenAsync(SerialSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CloseCurrent();

        SerialPort port = new(settings.PortName, settings.BaudRate)
        {
            DataBits = settings.DataBits,
            Parity = Enum.Parse<Parity>(settings.Parity, ignoreCase: true),
            StopBits = Enum.Parse<StopBits>(settings.StopBits, ignoreCase: true),
            ReadTimeout = 1000,
            WriteTimeout = 1000,
        };
        port.ErrorReceived += OnSerialError;
        port.Open();
        _port = port;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        SerialPort? port = _port ?? throw new InvalidOperationException("Port is not open.");
        await port.BaseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        await port.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        SerialPort? port = _port ?? throw new InvalidOperationException("Port is not open.");
        return await port.BaseStream.ReadAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DiscardInBufferAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _port?.DiscardInBuffer();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        CloseCurrent();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>Detach error reporting, close and dispose the port (best effort).</summary>
    private void CloseCurrent()
    {
        try
        {
            if (_port is not null)
            {
                _port.ErrorReceived -= OnSerialError;
                _port.Close();
            }
        }
        catch (IOException)
        {
            // Best effort close during re-open/dispose.
        }
        finally
        {
            _port?.Dispose();
            _port = null;
        }
    }

    /// <summary>Map port errors to line-error flags and raise <see cref="LineError"/>.</summary>
    /// <param name="sender">Error source (unused).</param>
    /// <param name="e">Serial error details (TXFull is ignored).</param>
    private void OnSerialError(object sender, SerialErrorReceivedEventArgs e)
    {
        SerialLineError error = e.EventType switch
        {
            SerialError.RXOver => SerialLineError.Overrun,
            SerialError.Overrun => SerialLineError.Overrun,
            SerialError.RXParity => SerialLineError.Parity,
            SerialError.Frame => SerialLineError.Frame,
            _ => SerialLineError.None,
        };

        if (error != SerialLineError.None)
        {
            LineError?.Invoke(this, error);
        }
    }
}
