using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC17 Read/Write Multiple Registers.</summary>
public static class ReadWriteMultipleCodec
{
    /// <summary>Encode request: [read start(2)][read count(2)][write start(2)][write count(2)][write byte count(1)][write data...].</summary>
    /// <param name="readStart">Zero-based start address of the registers to read (0..65535).</param>
    /// <param name="readCount">Number of registers to read, 1..125.</param>
    /// <param name="writeStart">Zero-based start address of the registers to write (0..65535).</param>
    /// <param name="writeValues">Register values to write in address order, 1..121 items.</param>
    /// <returns>Request body with big-endian read/write headers, a write byte count, and big-endian write data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writeValues"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="readCount"/> is outside 1..125 or <paramref name="writeValues"/> holds fewer than 1 or more than 121 items.</exception>
    public static byte[] EncodeRequest(ushort readStart, ushort readCount, ushort writeStart, IReadOnlyList<ushort> writeValues)
    {
        ArgumentNullException.ThrowIfNull(writeValues);
        ValidateReadCount(readCount);
        ValidateWriteCount(writeValues.Count);
        byte[] body = new byte[9 + (writeValues.Count * 2)];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), readStart);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), readCount);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(4, 2), writeStart);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(6, 2), (ushort)writeValues.Count);
        body[8] = (byte)(writeValues.Count * 2);
        for (int i = 0; i < writeValues.Count; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(9 + (i * 2), 2), writeValues[i]);
        }

        return body;
    }

    /// <summary>Decode request into read/write headers and write values.</summary>
    /// <param name="body">Request body holding big-endian read/write headers, a write byte count, and big-endian write data; at least 9 bytes long.</param>
    /// <param name="readStart">Receives the decoded zero-based read start address.</param>
    /// <param name="readCount">Receives the decoded number of registers to read.</param>
    /// <param name="writeStart">Receives the decoded zero-based write start address.</param>
    /// <param name="writeValues">Receives the decoded register values to write in address order.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is shorter than 9 bytes or its write byte count does not equal twice the write count.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the decoded read count is outside 1..125 or the write count is outside 1..121.</exception>
    public static void DecodeRequest(ReadOnlySpan<byte> body, out ushort readStart, out ushort readCount, out ushort writeStart, out ushort[] writeValues)
    {
        if (body.Length < 9)
        {
            throw new ArgumentException("FC17 request too short.", nameof(body));
        }

        readStart = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        readCount = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(2, 2));
        writeStart = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(4, 2));
        int writeCount = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(6, 2));
        ValidateReadCount(readCount);
        ValidateWriteCount(writeCount);
        int byteCount = body[8];
        if (byteCount != writeCount * 2 || body.Length != 9 + byteCount)
        {
            throw new ArgumentException("FC17 byte-count mismatch.", nameof(body));
        }

        writeValues = new ushort[writeCount];
        for (int i = 0; i < writeCount; i++)
        {
            writeValues[i] = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(9 + (i * 2), 2));
        }
    }

    /// <summary>Encode response ([byte count(1)][registers...]) — same layout as FC03/04.</summary>
    /// <param name="values">Register values in address order, 1..125 items.</param>
    /// <returns>Response body with a leading byte count followed by big-endian register data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> holds fewer than 1 or more than 125 items.</exception>
    public static byte[] EncodeResponse(IReadOnlyList<ushort> values) => ReadRegistersCodec.EncodeResponse(values);

    /// <summary>Decode response into registers.</summary>
    /// <param name="body">Response body starting with the byte count followed by big-endian register data.</param>
    /// <returns>Decoded register values in address order.</returns>
    /// <exception cref="ArgumentException">Thrown on byte-count mismatch.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the decoded register count is outside 1..125.</exception>
    public static ushort[] DecodeResponse(ReadOnlySpan<byte> body) => ReadRegistersCodec.DecodeResponse(body);

    /// <summary>Reject read counts outside 1..125.</summary>
    /// <param name="count">Count to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is outside 1..125.</exception>
    private static void ValidateReadCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ModbusLimits.MaxReadRegisters);
    }

    /// <summary>Reject write counts outside 1..121.</summary>
    /// <param name="count">Count to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is outside 1..121.</exception>
    private static void ValidateWriteCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ModbusLimits.MaxWriteRegisters17);
    }
}
