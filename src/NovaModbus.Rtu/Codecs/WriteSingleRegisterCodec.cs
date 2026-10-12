using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC06 Write Single Register. Request and response share the echo layout.</summary>
public static class WriteSingleRegisterCodec
{
    /// <summary>Encode body: [address BE(2)][value BE(2)].</summary>
    /// <param name="address">Zero-based register address (0..65535).</param>
    /// <param name="value">Register value to write (0..65535).</param>
    /// <returns>4-byte body shared by the FC06 request and response echo.</returns>
    public static byte[] Encode(ushort address, ushort value)
    {
        byte[] body = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), address);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), value);
        return body;
    }

    /// <summary>Decode body into address/value.</summary>
    /// <param name="body">4-byte body holding the big-endian register address and value.</param>
    /// <param name="address">Receives the decoded zero-based register address.</param>
    /// <param name="value">Receives the decoded register value.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is not 4 bytes long.</exception>
    public static void Decode(ReadOnlySpan<byte> body, out ushort address, out ushort value)
    {
        if (body.Length != 4)
        {
            throw new ArgumentException("FC06 body must be 4 bytes.", nameof(body));
        }

        address = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        value = BinaryPrimitives.ReadUInt16BigEndian(body[2..]);
    }
}
