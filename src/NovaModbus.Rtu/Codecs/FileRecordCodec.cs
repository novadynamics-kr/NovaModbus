using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>One file-record sub-request (reference type is always 6).</summary>
/// <param name="FileNumber">0x0000-0xFFFF.</param>
/// <param name="RecordNumber">0x0000-0x270F.</param>
/// <param name="Length">Registers to transfer.</param>
public sealed record FileSubRequest(ushort FileNumber, ushort RecordNumber, ushort Length);

/// <summary>FC14 Read File Record + FC15 Write File Record sub-request framing.</summary>
public static class FileRecordCodec
{
    private const byte ReferenceType = 0x06;
    private const int SubRequestSize = 7;

    /// <summary>Encode an FC14 request: [byte count(1)][7-byte sub-requests...].</summary>
    public static byte[] EncodeReadRequest(IReadOnlyList<FileSubRequest> subs)
    {
        ArgumentNullException.ThrowIfNull(subs);
        ValidateSubCount(subs.Count);
        byte[] body = new byte[1 + (subs.Count * SubRequestSize)];
        body[0] = (byte)(subs.Count * SubRequestSize);
        for (int i = 0; i < subs.Count; i++)
        {
            ValidateReadSub(subs[i]);
            WriteSub(body.AsSpan(1 + (i * SubRequestSize)), subs[i]);
        }

        return body;
    }

    /// <summary>Decode an FC14 request into sub-requests.</summary>
    public static FileSubRequest[] DecodeReadRequest(ReadOnlySpan<byte> body)
    {
        FileSubRequest[] subs = DecodeSubs(body);
        foreach (FileSubRequest sub in subs)
        {
            ValidateReadSub(sub);
        }

        return subs;
    }

    /// <summary>Encode an FC14 response: [byte count(1)]([data length(1)][data...] per record).</summary>
    public static byte[] EncodeReadResponse(IReadOnlyList<ushort[]> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        ValidateSubCount(records.Count);
        int total = records.Sum(r => 1 + (r.Length * 2));
        if (total > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(records), "Response too long.");
        }

        byte[] body = new byte[1 + total];
        body[0] = (byte)total;
        int offset = 1;
        foreach (ushort[] record in records)
        {
            ValidateReadLength(record.Length);
            body[offset++] = (byte)(record.Length * 2);
            foreach (ushort value in record)
            {
                BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(offset, 2), value);
                offset += 2;
            }
        }

        return body;
    }

    /// <summary>Decode an FC14 response into per-record register arrays.</summary>
    public static ushort[][] DecodeReadResponse(ReadOnlySpan<byte> body)
    {
        if (body.Length < 1 || body[0] != body.Length - 1)
        {
            throw new ArgumentException("FC14 byte-count mismatch.", nameof(body));
        }

        List<ushort[]> records = [];
        int offset = 1;
        while (offset < body.Length)
        {
            int dataLength = body[offset++];
            if (dataLength % 2 != 0 || offset + dataLength > body.Length)
            {
                throw new ArgumentException("FC14 record length mismatch.", nameof(body));
            }

            ushort[] record = new ushort[dataLength / 2];
            for (int i = 0; i < record.Length; i++)
            {
                record[i] = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(offset + (i * 2), 2));
            }

            ValidateReadLength(record.Length);
            records.Add(record);
            offset += dataLength;
        }

        ValidateSubCount(records.Count);
        return [.. records];
    }

    /// <summary>Encode an FC15 request: [byte count(1)]([7-byte sub][data length(1)][data...] per item).</summary>
    public static byte[] EncodeWriteRequest(IReadOnlyList<FileSubRequest> subs, IReadOnlyList<ushort[]> datas)
    {
        ArgumentNullException.ThrowIfNull(subs);
        ArgumentNullException.ThrowIfNull(datas);
        if (subs.Count != datas.Count)
        {
            throw new ArgumentException("Sub-request and data counts must match.", nameof(datas));
        }

        ValidateSubCount(subs.Count);
        int total = 0;
        for (int i = 0; i < subs.Count; i++)
        {
            ValidateWriteSub(subs[i], datas[i]);
            total += SubRequestSize + 1 + (datas[i].Length * 2);
        }

        if (total > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(subs), "Request too long.");
        }

        byte[] body = new byte[1 + total];
        body[0] = (byte)total;
        int offset = 1;
        foreach ((FileSubRequest sub, ushort[] data) in subs.Zip(datas))
        {
            WriteSub(body.AsSpan(offset, SubRequestSize), sub);
            offset += SubRequestSize;
            body[offset++] = (byte)(data.Length * 2);
            foreach (ushort value in data)
            {
                BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(offset, 2), value);
                offset += 2;
            }
        }

        return body;
    }

    /// <summary>Decode an FC15 request into sub-requests and per-item data.</summary>
    public static void DecodeWriteRequest(ReadOnlySpan<byte> body, out FileSubRequest[] subs, out ushort[][] datas)
    {
        if (body.Length < 1 || body[0] != body.Length - 1)
        {
            throw new ArgumentException("FC15 byte-count mismatch.", nameof(body));
        }

        List<FileSubRequest> subList = [];
        List<ushort[]> dataList = [];
        int offset = 1;
        while (offset < body.Length)
        {
            if (offset + SubRequestSize + 1 > body.Length)
            {
                throw new ArgumentException("FC15 sub-request truncated.", nameof(body));
            }

            FileSubRequest sub = ReadSub(body.Slice(offset, SubRequestSize));
            offset += SubRequestSize;
            int dataLength = body[offset++];
            if (dataLength % 2 != 0 || offset + dataLength > body.Length)
            {
                throw new ArgumentException("FC15 data length mismatch.", nameof(body));
            }

            ushort[] data = new ushort[dataLength / 2];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(offset + (i * 2), 2));
            }

            offset += dataLength;
            ValidateWriteSub(sub, data);
            subList.Add(sub);
            dataList.Add(data);
        }

        ValidateSubCount(subList.Count);
        subs = [.. subList];
        datas = [.. dataList];
    }

    /// <summary>Encode an FC15 response (echo of the sub-request section).</summary>
    public static byte[] EncodeWriteResponse(IReadOnlyList<FileSubRequest> subs)
    {
        ArgumentNullException.ThrowIfNull(subs);
        ValidateSubCount(subs.Count);
        byte[] body = new byte[1 + (subs.Count * SubRequestSize)];
        body[0] = (byte)(subs.Count * SubRequestSize);
        for (int i = 0; i < subs.Count; i++)
        {
            WriteSub(body.AsSpan(1 + (i * SubRequestSize)), subs[i]);
        }

        return body;
    }

    /// <summary>Decode an FC15 response into the echoed sub-requests.</summary>
    public static FileSubRequest[] DecodeWriteResponse(ReadOnlySpan<byte> body) => DecodeSubs(body);

    private static FileSubRequest[] DecodeSubs(ReadOnlySpan<byte> body)
    {
        if (body.Length < 1 + SubRequestSize || body[0] != body.Length - 1 || ((body.Length - 1) % SubRequestSize) != 0)
        {
            throw new ArgumentException("File sub-request section mismatch.", nameof(body));
        }

        int count = (body.Length - 1) / SubRequestSize;
        ValidateSubCount(count);
        FileSubRequest[] subs = new FileSubRequest[count];
        for (int i = 0; i < count; i++)
        {
            subs[i] = ReadSub(body.Slice(1 + (i * SubRequestSize), SubRequestSize));
        }

        return subs;
    }

    private static void WriteSub(Span<byte> destination, FileSubRequest sub)
    {
        destination[0] = ReferenceType;
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(1, 2), sub.FileNumber);
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(3, 2), sub.RecordNumber);
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(5, 2), sub.Length);
    }

    private static FileSubRequest ReadSub(ReadOnlySpan<byte> source)
    {
        if (source[0] != ReferenceType)
        {
            throw new ArgumentException("Reference type must be 6.", nameof(source));
        }

        return new FileSubRequest(
            BinaryPrimitives.ReadUInt16BigEndian(source.Slice(1, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(source.Slice(3, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(source.Slice(5, 2)));
    }

    private static void ValidateSubCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, ModbusLimits.MaxFileSubRequests);
    }

    private static void ValidateReadSub(FileSubRequest sub)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sub.RecordNumber, (ushort)ModbusLimits.MaxFileRecordNumber);
        ValidateReadLength(sub.Length);
    }

    private static void ValidateReadLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, ModbusLimits.MaxFileReadLength);
    }

    private static void ValidateWriteSub(FileSubRequest sub, ushort[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sub.RecordNumber, (ushort)ModbusLimits.MaxFileRecordNumber);
        ArgumentOutOfRangeException.ThrowIfLessThan(data.Length, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.Length, ModbusLimits.MaxFileWriteLength);
        if (sub.Length != data.Length)
        {
            throw new ArgumentException("Sub-request length must match data length.", nameof(data));
        }
    }
}
