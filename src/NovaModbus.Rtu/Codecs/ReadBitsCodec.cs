using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC01 Read Coils + FC02 Read Discrete Inputs (identical PDU layout).</summary>
public static class ReadBitsCodec
{
    /// <summary>Encode request body: [start BE(2)][count BE(2)].</summary>
    /// <param name="start">Zero-based start address (0..65535).</param>
    /// <param name="count">Bits to read, 1..2000.</param>
    /// <returns>4-byte request body.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is outside 1..2000.</exception>
    public static byte[] EncodeRequest(ushort start, ushort count)
    {
        ValidateCount(count, ModbusLimits.MaxReadBits);
        byte[] body = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), start);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), count);
        return body;
    }

    /// <summary>Decode request body into start/count.</summary>
    /// <param name="body">4-byte request body holding the big-endian start address and bit count.</param>
    /// <param name="start">Receives the decoded zero-based start address.</param>
    /// <param name="count">Receives the decoded bit count.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is not 4 bytes long.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the decoded count is outside 1..2000.</exception>
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
    /// <param name="values">Bit states in address order, 1..2000 items.</param>
    /// <returns>Response body with a leading byte count followed by LSB-first packed bits.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> holds fewer than 1 or more than 2000 items.</exception>
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
    /// <param name="body">Response body starting with the byte count followed by LSB-first packed bits.</param>
    /// <returns>Unpacked bit states in address order, one per bit of the payload (byte count times 8).</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is empty or its length does not match the leading byte count.</exception>
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
    /// <param name="body">Response body starting with the byte count followed by LSB-first packed bits.</param>
    /// <param name="count">Expected number of bits, 1..2000. Trailing padding bits must be zero.</param>
    /// <returns>The first <paramref name="count"/> unpacked bit states in address order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is outside 1..2000.</exception>
    /// <exception cref="ArgumentException">Thrown on byte-count mismatch, when the payload holds fewer bits than requested, or when a padding bit is non-zero.</exception>
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

    /// <summary>Reject bit counts outside 1..max.</summary>
    /// <param name="count">Count to validate.</param>
    /// <param name="max">Maximum allowed count.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is outside 1..<paramref name="max"/>.</exception>
    /// <summary>Reject counts outside 1..max.</summary>
    /// <param name="count">Count to validate.</param>
    /// <param name="max">Inclusive upper bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when out of range.</exception>
    private static void ValidateCount(int count, int max)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, max);
    }
}
