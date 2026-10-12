using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC18 Read FIFO Queue. Request is [pointer address BE(2)]; response is [byte count BE(2)][FIFO count BE(2)][values...].</summary>
public static class FifoQueueCodec
{
    /// <summary>Encode request body.</summary>
    /// <param name="pointerAddress">FIFO pointer address (0x0000-0xFFFF) of the queue descriptor.</param>
    /// <returns>A 2-byte big-endian encoding of <paramref name="pointerAddress"/>.</returns>
    public static byte[] EncodeRequest(ushort pointerAddress)
    {
        byte[] body = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(body, pointerAddress);
        return body;
    }

    /// <summary>Decode request body.</summary>
    /// <param name="body">Request body; must be exactly 2 bytes holding the big-endian pointer address.</param>
    /// <returns>The FIFO pointer address.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is not exactly 2 bytes.</exception>
    public static ushort DecodeRequest(ReadOnlySpan<byte> body)
    {
        if (body.Length != 2)
        {
            throw new ArgumentException("FC18 request must be 2 bytes.", nameof(body));
        }

        return BinaryPrimitives.ReadUInt16BigEndian(body);
    }

    /// <summary>Encode response body (non-destructive read of up to 31 values).</summary>
    /// <param name="values">Queue values to encode (0-31 registers).</param>
    /// <returns>The encoded body: [byte count BE(2)][FIFO count BE(2)][big-endian values...].</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> holds more than 31 entries.</exception>
    public static byte[] EncodeResponse(IReadOnlyList<ushort> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count > ModbusLimits.MaxFifoCount)
        {
            throw new ArgumentOutOfRangeException(nameof(values), $"At most {ModbusLimits.MaxFifoCount} FIFO values.");
        }

        byte[] body = new byte[4 + (values.Count * 2)];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), (ushort)(2 + (values.Count * 2)));
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), (ushort)values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(4 + (i * 2), 2), values[i]);
        }

        return body;
    }

    /// <summary>Decode response body into queue values.</summary>
    /// <param name="body">Response body; at least 4 bytes: [byte count BE(2)][FIFO count BE(2)][values...] with consistent counts.</param>
    /// <returns>The decoded queue register values.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is too short or its byte/FIFO counts mismatch.</exception>
    public static ushort[] DecodeResponse(ReadOnlySpan<byte> body)
    {
        if (body.Length < 4)
        {
            throw new ArgumentException("FC18 response too short.", nameof(body));
        }

        int byteCount = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        int count = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(2, 2));
        if (byteCount != 2 + (count * 2) || body.Length != 2 + byteCount || count > ModbusLimits.MaxFifoCount)
        {
            throw new ArgumentException("FC18 byte-count mismatch.", nameof(body));
        }

        ushort[] values = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(4 + (i * 2), 2));
        }

        return values;
    }
}
