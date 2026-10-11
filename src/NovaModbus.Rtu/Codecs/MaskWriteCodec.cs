using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>Function 0x16 Mask Write Register. Request and response share the echo layout: [address BE(2)][AND mask BE(2)][OR mask BE(2)].</summary>
public static class MaskWriteCodec
{
    /// <summary>Encode body.</summary>
    public static byte[] Encode(ushort address, ushort andMask, ushort orMask)
    {
        byte[] body = new byte[6];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), address);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), andMask);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(4, 2), orMask);
        return body;
    }

    /// <summary>Decode body.</summary>
    public static void Decode(ReadOnlySpan<byte> body, out ushort address, out ushort andMask, out ushort orMask)
    {
        if (body.Length != 6)
        {
            throw new ArgumentException("FC16 body must be 6 bytes.", nameof(body));
        }

        address = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        andMask = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(2, 2));
        orMask = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(4, 2));
    }

    /// <summary>Apply the mask algorithm: result = (current AND andMask) OR (orMask AND NOT andMask).</summary>
    public static ushort Apply(ushort current, ushort andMask, ushort orMask) =>
        (ushort)((current & andMask) | (orMask & ~andMask));
}
