using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC01 Read Coils + FC02 Read Discrete Inputs (identical PDU layout).</summary>
public static class ReadBitsCodec
{
    /// <summary>Encode request body: [start BE(2)][count BE(2)].</summary>
    public static byte[] EncodeRequest(ushort start, ushort count)
    {
        ValidateCount(count, ModbusLimits.MaxReadBits);
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
            throw new ArgumentException("Read-bits request must be 4 bytes.", nameof(body));
        }

        start = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        count = BinaryPrimitives.ReadUInt16BigEndian(body[2..]);
        ValidateCount(count, ModbusLimits.MaxReadBits);
    }

    /// <summary>Encode response body: [byteCount(1)][packed...].</summary>
    public static byte[] EncodeResponse(IReadOnlyList<bool> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ValidateCount(values.Count, ModbusLimits.MaxReadBits);
        byte[] packed = ModbusBitPacker.Pack(values);
        byte[] body = new byte[1 + packed.Length];
        body[0] = (byte)packed.Length;
        packed.CopyTo(body, 1);
        return body;
    }

    /// <summary>Decode response body into bool values.</summary>
    public static bool[] DecodeResponse(ReadOnlySpan<byte> body)
    {
        if (body.Length < 1)
        {
            throw new ArgumentException("Read-bits response needs byte-count.", nameof(body));
        }

        int byteCount = body[0];
        if (body.Length != 1 + byteCount)
        {
            throw new ArgumentException("Byte-count mismatch.", nameof(body));
        }

        int maxBits = byteCount * 8;
        return ModbusBitPacker.Unpack(body.Slice(1, byteCount), maxBits);
    }

    /// <summary>Decode response when the expected count is known (validates padding).</summary>
    public static bool[] DecodeResponse(ReadOnlySpan<byte> body, int count)
    {
        ValidateCount(count, ModbusLimits.MaxReadBits);
        bool[] all = DecodeResponse(body);
        if (all.Length < count)
        {
            throw new ArgumentException("Response holds fewer bits than requested.", nameof(body));
        }

        // Bits beyond count must be zero.
        for (int i = count; i < all.Length; i++)
        {
            if (all[i])
            {
                throw new ArgumentException("Non-zero padding bit in response.", nameof(body));
            }
        }

        return all[..count];
    }

    private static void ValidateCount(int count, int max)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, max);
    }
}
