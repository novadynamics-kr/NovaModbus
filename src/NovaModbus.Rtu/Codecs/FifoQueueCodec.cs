using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC18 Read FIFO Queue. Request is [pointer address BE(2)]; response is [byte count BE(2)][FIFO count BE(2)][values...].</summary>
public static class FifoQueueCodec
{
    /// <summary>Encode request body.</summary>
    public static byte[] EncodeRequest(ushort pointerAddress)
    {
        byte[] body = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(body, pointerAddress);
        return body;
    }

    /// <summary>Decode request body.</summary>
    public static ushort DecodeRequest(ReadOnlySpan<byte> body)
    {
        if (body.Length != 2)
        {
            throw new ArgumentException("FC18 request must be 2 bytes.", nameof(body));
        }

        return BinaryPrimitives.ReadUInt16BigEndian(body);
    }

    /// <summary>Encode response body (non-destructive read of up to 31 values).</summary>
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
