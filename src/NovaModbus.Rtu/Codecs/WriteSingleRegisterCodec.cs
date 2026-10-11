using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC06 Write Single Register. Request and response share the echo layout.</summary>
public static class WriteSingleRegisterCodec
{
    /// <summary>Encode body: [address BE(2)][value BE(2)].</summary>
    public static byte[] Encode(ushort address, ushort value)
    {
        byte[] body = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), address);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), value);
        return body;
    }

    /// <summary>Decode body into address/value.</summary>
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
