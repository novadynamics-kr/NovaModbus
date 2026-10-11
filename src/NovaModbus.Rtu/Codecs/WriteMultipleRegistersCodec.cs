using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC16 Write Multiple Registers.</summary>
public static class WriteMultipleRegistersCodec
{
    /// <summary>Encode request: [start BE(2)][count BE(2)][byteCount(1)][registers BE...].</summary>
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
    public static byte[] EncodeResponse(ushort start, ushort count)
    {
        ValidateCount(count);
        byte[] body = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), start);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), count);
        return body;
    }

    /// <summary>Decode response into start/count.</summary>
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

    private static void ValidateCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ModbusLimits.MaxWriteRegisters);
    }
}
