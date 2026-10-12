using System.Diagnostics;

namespace NovaModbus.Rtu.Transport;

/// <summary>RTU timing helper and silent-interval frame reader.</summary>
public static class RtuFramer
{
    private const int BitsPerChar = 11; // start + 8 data + parity + stop (worst case)
    private const int ReadChunkSize = 256;

    /// <summary>Max RTU ADU size in bytes (address + function + data + CRC).</summary>
    public const int MaxFrameSize = 256;

    /// <summary>Compute 3.5-char silence for the given baud rate.</summary>
    /// <remarks>
    /// Baud rates above 19200 use the fixed 1.75ms value; at or below 19200 the
    /// exact 3.5-char time applies with a 2ms floor.
    /// </remarks>
    /// <param name="baudRate">Baud rate in bits per second.</param>
    /// <returns>The 3.5-character silent interval for the given baud rate.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="baudRate"/> is not positive.</exception>
    public static TimeSpan SilentInterval(int baudRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baudRate, 0);
        if (baudRate > 19200)
        {
            return TimeSpan.FromMilliseconds(1.75);
        }

        double ms = (BitsPerChar * 3.5 / baudRate) * 1000.0;
        return TimeSpan.FromMilliseconds(Math.Max(ms, 2.0));
    }

    /// <summary>Compute 1.5-char inter-character timeout for the given baud rate.</summary>
    /// <remarks>
    /// Baud rates above 19200 use the fixed 750µs value; at or below 19200 the
    /// exact 1.5-char time applies. A gap longer than this inside a frame marks
    /// the frame incomplete (discarded).
    /// </remarks>
    /// <param name="baudRate">Baud rate in bits per second.</param>
    /// <returns>The 1.5-character inter-character timeout for the given baud rate.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="baudRate"/> is not positive.</exception>
    public static TimeSpan InterCharInterval(int baudRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baudRate, 0);
        if (baudRate > 19200)
        {
            return TimeSpan.FromMilliseconds(0.75);
        }

        double ms = (BitsPerChar * 1.5 / baudRate) * 1000.0;
        return TimeSpan.FromMilliseconds(ms);
    }

    /// <summary>
    /// Read one RTU frame delimited by a silent interval.
    /// Returns null when no data arrives within <paramref name="timeout"/> or when
    /// the frame exceeds <see cref="MaxFrameSize"/> bytes (discarded per spec).
    /// Frames with an inter-character gap over t1.5 are discarded; reception
    /// restarts at the next byte. Does not validate CRC — the transaction layer does that.
    /// </summary>
    /// <param name="transport">Open serial transport.</param>
    /// <param name="baudRate">Used to derive the silent intervals.</param>
    /// <param name="timeout">Max wait for the first byte.</param>
    /// <param name="silentIntervalOverride">Test hook replacing the derived t3.5.</param>
    /// <param name="interCharIntervalOverride">Test hook replacing the derived t1.5.</param>
    /// <param name="cancellationToken">User cancellation (throws).</param>
    /// <returns>The received frame bytes, or null when no data arrives within the timeout or the frame is discarded as oversize.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transport"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="baudRate"/> is not positive, or <paramref name="timeout"/> is negative.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested via <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="EndOfStreamException">The transport closed (returned 0 bytes) while waiting for the first byte.</exception>
    public static async Task<byte[]?> ReadFrameAsync(
        ISerialTransport transport,
        int baudRate,
        TimeSpan timeout,
        TimeSpan? silentIntervalOverride = null,
        TimeSpan? interCharIntervalOverride = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baudRate, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);
        TimeSpan silence = silentIntervalOverride ?? SilentInterval(baudRate);
        TimeSpan interChar = interCharIntervalOverride ?? InterCharInterval(baudRate);

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        List<byte> frame = [];

        // Phase 1: wait for the first byte.
        timeoutCts.CancelAfter(timeout);
        int first = await ReadOnceAsync(transport, timeoutCts.Token).ConfigureAwait(false);
        if (first < 0)
        {
            // Timed out before any data (user cancel would have thrown).
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        frame.Add((byte)first);

        // Phase 2: accumulate until a full t3.5 silence with no data.
        // A gap over t1.5 but under t3.5 marks the frame incomplete: discard it
        // and restart reception at the newly arrived bytes.
        byte[] chunk = new byte[ReadChunkSize];
        Stopwatch sinceByte = Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TimeSpan toEnd = silence - sinceByte.Elapsed;
            if (toEnd <= TimeSpan.Zero)
            {
                // Full silence elapsed: frame complete.
                return [.. frame];
            }

            TimeSpan toGap = interChar - sinceByte.Elapsed;
            TimeSpan wait = toGap < toEnd ? toGap : toEnd;
            int read = await TryReadAsync(transport, chunk, wait, cancellationToken).ConfigureAwait(false);
            if (read >= 0)
            {
                if (read == 0)
                {
                    // End of stream: deliver what we have.
                    return [.. frame];
                }

                frame.AddRange(chunk.AsSpan(0, read).ToArray());
                if (frame.Count > MaxFrameSize)
                {
                    // Oversize ADU: discard per spec.
                    return null;
                }

                sinceByte.Restart();
                continue;
            }

            if (silence - sinceByte.Elapsed <= TimeSpan.Zero)
            {
                // t3.5 reached without data: frame complete.
                return [.. frame];
            }

            // t1.5 exceeded but t3.5 not reached: wait out the remainder.
            TimeSpan tail = silence - sinceByte.Elapsed;
            int late = await TryReadAsync(transport, chunk, tail, cancellationToken).ConfigureAwait(false);
            if (late < 0)
            {
                // Silence reached: the partial frame is complete as-is.
                return [.. frame];
            }

            if (late == 0)
            {
                // End of stream: deliver what we have.
                return [.. frame];
            }

            // Illegal inter-character gap: discard and restart at the late bytes.
            frame.Clear();
            frame.AddRange(chunk.AsSpan(0, late).ToArray());
            if (frame.Count > MaxFrameSize)
            {
                return null;
            }

            sinceByte.Restart();
        }
    }

    /// <summary>
    /// Read with a bounded wait. Returns bytes read (0 = end of stream),
    /// or -1 when the wait expired. User cancellation throws.
    /// </summary>
    private static async Task<int> TryReadAsync(
        ISerialTransport transport, byte[] chunk, TimeSpan wait, CancellationToken userToken)
    {
        if (wait <= TimeSpan.Zero)
        {
            return -1;
        }

        using CancellationTokenSource waitCts = CancellationTokenSource.CreateLinkedTokenSource(userToken);
        waitCts.CancelAfter(wait);
        try
        {
            return await transport.ReadAsync(chunk, waitCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!userToken.IsCancellationRequested)
        {
            // Wait expired with no data.
            return -1;
        }
        catch (TimeoutException)
        {
            // Transports with built-in read timeouts report silence this way.
            return -1;
        }
    }

    /// <summary>Read a single byte, translating transport timeouts into -1.</summary>
    /// <param name="transport">Open serial transport.</param>
    /// <param name="token">Linked wait/user token (user cancel still throws via the caller check).</param>
    /// <returns>The byte value, or -1 on timeout/cancel-silence.</returns>
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
