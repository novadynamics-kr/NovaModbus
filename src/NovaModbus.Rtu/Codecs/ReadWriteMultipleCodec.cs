using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC17 Read/Write Multiple Registers.</summary>
public static class ReadWriteMultipleCodec
{
    /// <summary>Encode request: [read start(2)][read count(2)][write start(2)][write count(2)][write byte count(1)][write data...].</summary>
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

    /// <summary>Decode request.</summary>
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
    public static byte[] EncodeResponse(IReadOnlyList<ushort> values) => ReadRegistersCodec.EncodeResponse(values);

    /// <summary>Decode response into registers.</summary>
    public static ushort[] DecodeResponse(ReadOnlySpan<byte> body) => ReadRegistersCodec.DecodeResponse(body);

    private static void ValidateReadCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ModbusLimits.MaxReadRegisters);
    }

    private static void ValidateWriteCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ModbusLimits.MaxWriteRegisters);
    }
}
