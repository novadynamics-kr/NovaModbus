using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC16 Write Multiple Registers.</summary>
public static class WriteMultipleRegistersCodec
{
    /// <summary>Encode request: [start BE(2)][count BE(2)][byteCount(1)][registers BE...].</summary>
    /// <param name="start">Zero-based start register address (0..65535).</param>
    /// <param name="values">Register values in address order, 1..123 items.</param>
    /// <returns>Request body with big-endian start/count, a byte count, and big-endian register data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> holds fewer than 1 or more than 123 items.</exception>
    public static byte[] EncodeRequest(ushort start, IReadOnlyList<ushort> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ValidateCount(values.Count);
        byte[] body = new byte[5 + (values.Count * 2)];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), start);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), (ushort)values.Count);
        body[4] = (byte)(values.Count * 2);
        for (int i = 0; i < values.Count; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(5 + (i * 2), 2), values[i]);
        }

        return body;
    }

    /// <summary>Decode request into start address and register values.</summary>
    /// <param name="body">Request body holding big-endian start/count, a byte count, and big-endian register data; at least 5 bytes long.</param>
    /// <param name="start">Receives the decoded zero-based start register address.</param>
    /// <param name="values">Receives the decoded register values in address order.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is shorter than 5 bytes or its byte count does not equal twice the register count.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the decoded register count is outside 1..123.</exception>
    public static void DecodeRequest(ReadOnlySpan<byte> body, out ushort start, out ushort[] values)
    {
        if (body.Length < 5)
        {
            throw new ArgumentException("FC16 request too short.", nameof(body));
        }

        start = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        int count = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(2, 2));
        ValidateCount(count);
        int byteCount = body[4];
        if (byteCount != count * 2 || body.Length != 5 + byteCount)
        {
            throw new ArgumentException("FC16 byte-count mismatch.", nameof(body));
        }

        values = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(5 + (i * 2), 2));
        }
    }

    /// <summary>Encode response: [start BE(2)][count BE(2)].</summary>
    /// <param name="start">Zero-based start register address (0..65535).</param>
    /// <param name="count">Number of registers written, 1..123.</param>
    /// <returns>4-byte response body echoing the start address and register count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is outside 1..123.</exception>
    public static byte[] EncodeResponse(ushort start, ushort count)
    {
        ValidateCount(count);
        byte[] body = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), start);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), count);
        return body;
    }

    /// <summary>Decode response into start/count.</summary>
    /// <param name="body">4-byte response body holding the big-endian start address and register count.</param>
    /// <param name="start">Receives the decoded zero-based start register address.</param>
    /// <param name="count">Receives the decoded number of registers written.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is not 4 bytes long.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the decoded count is outside 1..123.</exception>
    public static void DecodeResponse(ReadOnlySpan<byte> body, out ushort start, out ushort count)
    {
        if (body.Length != 4)
        {
            throw new ArgumentException("FC16 response must be 4 bytes.", nameof(body));
        }

        start = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        count = BinaryPrimitives.ReadUInt16BigEndian(body[2..]);
        ValidateCount(count);
    }

    /// <summary>Reject register counts outside 1..123.</summary>
    /// <param name="count">Count to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is outside 1..123.</exception>
    private static void ValidateCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ModbusLimits.MaxWriteRegisters);
    }
}
