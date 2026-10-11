namespace NovaModbus.Rtu.Transport;

/// <summary>RTU timing helper and silent-interval frame reader.</summary>
public static class RtuFramer
{
    private const int BitsPerChar = 11; // start + 8 data + parity + stop (worst case)
    private const int ReadChunkSize = 256;

    /// <summary>Compute 3.5-char silence for the given baud rate (min 2ms floor).</summary>
    public static TimeSpan SilentInterval(int baudRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baudRate, 0);
        double ms = (BitsPerChar * 3.5 / baudRate) * 1000.0;
        return TimeSpan.FromMilliseconds(Math.Max(ms, 2.0));
    }

    /// <summary>
    /// Read one RTU frame delimited by a silent interval.
    /// Returns null when no data arrives within <paramref name="timeout"/>.
    /// Does not validate length or CRC — the transaction layer does that.
    /// </summary>
    /// <param name="transport">Open serial transport.</param>
    /// <param name="baudRate">Used to derive the silent interval.</param>
    /// <param name="timeout">Max wait for the first byte.</param>
    /// <param name="cancellationToken">User cancellation (throws).</param>
    /// <param name="silentIntervalOverride">Test hook replacing the derived interval.</param>
    public static async Task<byte[]?> ReadFrameAsync(
        ISerialTransport transport,
        int baudRate,
        TimeSpan timeout,
        TimeSpan? silentIntervalOverride = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baudRate, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);
        TimeSpan silence = silentIntervalOverride ?? SilentInterval(baudRate);

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        List<byte> frame = [];

        // Phase 1: wait for the first byte.
        timeoutCts.CancelAfter(timeout);
        int first = await ReadOnceAsync(transport, timeoutCts.Token).ConfigureAwait(false);        if (first < 0)
        {
            // Timed out before any data (user cancel would have thrown).
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        frame.Add((byte)first);

        // Phase 2: accumulate until a silent interval with no data.
        byte[] chunk = new byte[ReadChunkSize];
        while (true)
        {
            timeoutCts.CancelAfter(silence);
            int read;
            try
            {
                read = await transport.ReadAsync(chunk, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Silent interval elapsed with no data: frame complete.
                return [.. frame];
            }
            catch (TimeoutException)
            {
                // Transports with built-in read timeouts report silence this way.
                return [.. frame];
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (read == 0)
            {
                // End of stream: deliver what we have.
                return [.. frame];
            }

            frame.AddRange(chunk.AsSpan(0, read).ToArray());
        }
    }

    private static async Task<int> ReadOnceAsync(ISerialTransport transport, CancellationToken token)
    {
        byte[] single = new byte[1];
        try
        {
            int read = await transport.ReadAsync(single, token).ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException("Transport closed.");
            }

            return single[0];
        }
        catch (OperationCanceledException)
        {
            return -1;
        }
        catch (TimeoutException)
        {
            // Transports with built-in read timeouts (e.g. SerialPort) report
            // silence this way instead of honoring cancellation.
            return -1;
        }
    }
}
