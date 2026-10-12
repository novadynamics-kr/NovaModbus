using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC15 Write Multiple Coils.</summary>
public static class WriteMultipleCoilsCodec
{
    /// <summary>Encode request: [start BE(2)][count BE(2)][byteCount(1)][packed...].</summary>
    /// <param name="start">Zero-based start coil address (0..65535).</param>
    /// <param name="values">Coil states in address order, 1..1968 items.</param>
    /// <returns>Request body with big-endian start/count, a byte count, and LSB-first packed coil bits.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> holds fewer than 1 or more than 1968 items.</exception>
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
    /// <param name="body">Request body holding big-endian start/count, a byte count, and LSB-first packed coil bits; at least 5 bytes long.</param>
    /// <param name="start">Receives the decoded zero-based start coil address.</param>
    /// <param name="values">Receives the decoded coil states in address order.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is shorter than 5 bytes or its byte count does not match the coil count.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the decoded coil count is outside 1..1968.</exception>
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
    /// <param name="start">Zero-based start coil address (0..65535).</param>
    /// <param name="count">Number of coils written, 1..1968.</param>
    /// <returns>4-byte response body echoing the start address and coil count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is outside 1..1968.</exception>
    public static byte[] EncodeResponse(ushort start, ushort count)
    {
        ValidateCount(count);
        byte[] body = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), start);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), count);
        return body;
    }

    /// <summary>Decode response into start/count.</summary>
    /// <param name="body">4-byte response body holding the big-endian start address and coil count.</param>
    /// <param name="start">Receives the decoded zero-based start coil address.</param>
    /// <param name="count">Receives the decoded number of coils written.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is not 4 bytes long.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the decoded count is outside 1..1968.</exception>
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

    /// <summary>Reject coil counts outside 1..1968.</summary>
    /// <param name="count">Count to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is outside 1..1968.</exception>
    private static void ValidateCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ModbusLimits.MaxWriteCoils);
    }
}
