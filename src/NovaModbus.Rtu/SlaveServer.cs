namespace NovaModbus.Rtu;

/// <summary>Background serve loop for a configured slave (demo/test hosting).</summary>
public sealed class SlaveServer
{
    private readonly ModbusRtuSlave _slave;

    /// <summary>Initializes a server driving the given slave.</summary>
    public SlaveServer(ModbusRtuSlave slave)
    {
        ArgumentNullException.ThrowIfNull(slave);
        _slave = slave;
    }

    /// <summary>Last loop error (null when healthy). Set when the loop stops on error.</summary>
    public Exception? LastError { get; private set; }

    /// <summary>Serve requests until cancelled. Never throws for cancellation.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _slave.ServeOnceAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LastError = ex;
                break;
            }
        }
    }
}
