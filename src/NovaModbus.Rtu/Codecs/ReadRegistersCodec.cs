using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC03 Read Holding Registers + FC04 Read Input Registers (identical PDU layout).</summary>
public static class ReadRegistersCodec
{
    /// <summary>Encode request body: [start BE(2)][count BE(2)].</summary>
    public static byte[] EncodeRequest(ushort start, ushort count)
    {
        ValidateCount(count);
        byte[] body = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), start);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), count);
        return body;
    }

    /// <summary>Decode request body into start/count.</summary>
    public static void DecodeRequest(ReadOnlySpan<byte> body, out ushort start, out ushort count)
    {
        if (body.Length != 4)
        {
            throw new ArgumentException("Read-registers request must be 4 bytes.", nameof(body));
        }

        start = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        count = BinaryPrimitives.ReadUInt16BigEndian(body[2..]);
        ValidateCount(count);
    }

    /// <summary>Encode response body: [byteCount(1)][registers BE...].</summary>
    public static byte[] EncodeResponse(IReadOnlyList<ushort> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ValidateCount(values.Count);
        byte[] body = new byte[1 + (values.Count * 2)];
        body[0] = (byte)(values.Count * 2);
        for (int i = 0; i < values.Count; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(1 + (i * 2), 2), values[i]);
        }

        return body;
    }

    /// <summary>Decode response body into registers.</summary>
    public static ushort[] DecodeResponse(ReadOnlySpan<byte> body)
    {
        if (body.Length < 1)
        {
            throw new ArgumentException("Read-registers response needs byte-count.", nameof(body));
        }

        int byteCount = body[0];
        if (byteCount % 2 != 0 || body.Length != 1 + byteCount)
        {
            throw new ArgumentException("Byte-count mismatch.", nameof(body));
        }

        int count = byteCount / 2;
        ValidateCount(count);
        ushort[] values = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(1 + (i * 2), 2));
        }

        return values;
    }

    private static void ValidateCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ModbusLimits.MaxReadRegisters);
    }
}
