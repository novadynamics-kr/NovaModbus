using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC15 Write Multiple Coils.</summary>
public static class WriteMultipleCoilsCodec
{
    /// <summary>Encode request: [start BE(2)][count BE(2)][byteCount(1)][packed...].</summary>
    public static byte[] EncodeRequest(ushort start, IReadOnlyList<bool> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ValidateCount(values.Count);
        byte[] packed = ModbusBitPacker.Pack(values);
        byte[] body = new byte[5 + packed.Length];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), start);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), (ushort)values.Count);
        body[4] = (byte)packed.Length;
        packed.CopyTo(body, 5);
        return body;
    }

    /// <summary>Decode request into start address and coil values.</summary>
    public static void DecodeRequest(ReadOnlySpan<byte> body, out ushort start, out bool[] values)
    {
        if (body.Length < 5)
        {
            throw new ArgumentException("FC15 request too short.", nameof(body));
        }

        start = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        int count = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(2, 2));
        ValidateCount(count);
        int byteCount = body[4];
        if (byteCount != ((count + 7) / 8) || body.Length != 5 + byteCount)
        {
            throw new ArgumentException("FC15 byte-count mismatch.", nameof(body));
        }

        values = ModbusBitPacker.Unpack(body.Slice(5, byteCount), count);
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
            throw new ArgumentException("FC15 response must be 4 bytes.", nameof(body));
        }

        start = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        count = BinaryPrimitives.ReadUInt16BigEndian(body[2..]);
        ValidateCount(count);
    }

    private static void ValidateCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ModbusLimits.MaxWriteCoils);
    }
}
