using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>Function 0x16 Mask Write Register. Request and response share the echo layout: [address BE(2)][AND mask BE(2)][OR mask BE(2)].</summary>
public static class MaskWriteCodec
{
    /// <summary>Encode body: [address BE(2)][AND mask BE(2)][OR mask BE(2)].</summary>
    /// <param name="address">Zero-based register address (0..65535).</param>
    /// <param name="andMask">AND mask applied to the current register value first.</param>
    /// <param name="orMask">OR mask applied second, masked by the complement of <paramref name="andMask"/>.</param>
    /// <returns>6-byte body shared by the mask-write request and response echo.</returns>
    public static byte[] Encode(ushort address, ushort andMask, ushort orMask)
    {
        byte[] body = new byte[6];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), address);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), andMask);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(4, 2), orMask);
        return body;
    }

    /// <summary>Decode body into address and masks.</summary>
    /// <param name="body">6-byte body holding the big-endian address, AND mask, and OR mask.</param>
    /// <param name="address">Receives the decoded zero-based register address.</param>
    /// <param name="andMask">Receives the decoded AND mask.</param>
    /// <param name="orMask">Receives the decoded OR mask.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is not 6 bytes long.</exception>
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
    /// <param name="current">Current register value.</param>
    /// <param name="andMask">AND mask applied to <paramref name="current"/> first.</param>
    /// <param name="orMask">OR mask applied second, masked by the complement of <paramref name="andMask"/>.</param>
    /// <returns>The masked result register value.</returns>
    public static ushort Apply(ushort current, ushort andMask, ushort orMask) =>
        (ushort)((current & andMask) | (orMask & ~andMask));
}
