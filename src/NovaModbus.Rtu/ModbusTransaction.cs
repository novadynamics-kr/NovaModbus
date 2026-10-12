using System.Diagnostics;
using NovaModbus.Rtu.Enums;
using NovaModbus.Rtu.Errors;
using NovaModbus.Rtu.Logging;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu;

/// <summary>Single request/response exchange over an RTU line.</summary>
public static class ModbusTransaction
{
    /// <summary>
    /// Send one request and wait for its response.
    /// Garbage (CRC mismatch), foreign slave IDs, and unexpected functions are
    /// ignored while the attempt timeout lasts. Slave exception responses raise
    /// <see cref="ModbusSlaveException"/>. Returns the response PDU body.
    /// </summary>
    /// <param name="transport">Open serial transport.</param>
    /// <param name="baudRate">Line baud rate used for frame timing.</param>
    /// <param name="slaveId">Target slave ID (1-247; broadcast is rejected).</param>
    /// <param name="function">Function code byte.</param>
    /// <param name="requestBody">Function PDU body (CRC excluded).</param>
    /// <param name="timeout">Per-attempt response wait budget.</param>
    /// <param name="retries">Resend count after a timed-out attempt (0 = try once).</param>
    /// <param name="silentIntervalOverride">Test hook replacing the derived t3.5.</param>
    /// <param name="logger">Traffic logger; null selects the null logger.</param>
    /// <param name="cancellationToken">Cancellation token (throws on cancel).</param>
    /// <returns>Response PDU body (function echo verified).</returns>
    /// <exception cref="ModbusSlaveException">Thrown on a matching exception response.</exception>
    /// <exception cref="TimeoutException">Thrown when all attempts expire without a matching response.</exception>
    public static async Task<byte[]> ExchangeAsync(
        ISerialTransport transport,
        int baudRate,
        byte slaveId,
        byte function,
        byte[] requestBody,
        TimeSpan timeout,
        int retries = 1,
        TimeSpan? silentIntervalOverride = null,
        IModbusLogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(requestBody);
        ArgumentOutOfRangeException.ThrowIfLessThan(slaveId, (byte)1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slaveId, (byte)247);
        ArgumentOutOfRangeException.ThrowIfLessThan(retries, 0);
        IModbusLogger log = logger ?? NullModbusLogger.Instance;

        byte[] request = new ModbusFrame(slaveId, function, requestBody).Encode();
        for (int attempt = 0; ; attempt++)
        {
            await transport.WriteAsync(request, cancellationToken).ConfigureAwait(false);
            log.LogTx(request);
            long start = Stopwatch.GetTimestamp();

            while (true)
            {
                TimeSpan remaining = timeout - Stopwatch.GetElapsedTime(start);
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                byte[]? raw = await RtuFramer.ReadFrameAsync(
                    transport, baudRate, remaining, silentIntervalOverride, null, cancellationToken).ConfigureAwait(false);
                if (raw is null)
                {
                    break;
                }

                if (!ModbusFrame.TryDecode(raw, out ModbusFrame? frame) || frame is null)
                {
                    log.LogInfo($"Discarded {raw.Length} garbage bytes.");
                    continue;
                }

                log.LogRx(raw);
                if (frame.SlaveId != slaveId)
                {
                    continue;
                }

                if (frame.IsException)
                {
                    if (frame.BaseFunction == (ModbusFunction)function && frame.Data.Length == 1)
                    {
                        throw new ModbusSlaveException(frame.Data[0]);
                    }

                    continue;
                }

                if (frame.Function != function)
                {
                    continue;
                }

                return frame.Data;
            }

            if (attempt >= retries)
            {
                throw new TimeoutException(
                    $"No response from slave {slaveId} (FC 0x{function:X2}) after {attempt + 1} attempt(s).");
            }
        }
    }
}
