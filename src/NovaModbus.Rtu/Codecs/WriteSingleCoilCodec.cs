using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC05 Write Single Coil. Request and response share the echo layout.</summary>
public static class WriteSingleCoilCodec
{
    /// <summary>Coil ON value (0xFF00).</summary>
    public const ushort OnValue = 0xFF00;

    /// <summary>Coil OFF value (0x0000).</summary>
    public const ushort OffValue = 0x0000;

    /// <summary>Encode body: [address BE(2)][0xFF00|0x0000 BE(2)].</summary>
    public static byte[] Encode(ushort address, bool on)
    {
        byte[] body = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), address);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), on ? OnValue : OffValue);
        return body;
    }

    /// <summary>Decode body into address/value. Rejects values other than FF00/0000.</summary>
    public static void Decode(ReadOnlySpan<byte> body, out ushort address, out bool on)
    {
        if (body.Length != 4)
        {
            throw new ArgumentException("FC05 body must be 4 bytes.", nameof(body));
        }

        address = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        ushort raw = BinaryPrimitives.ReadUInt16BigEndian(body[2..]);
        on = raw switch
        {
            OnValue => true,
            OffValue => false,
            _ => throw new ArgumentException("FC05 value must be 0xFF00 or 0x0000.", nameof(body)),
        };
    }
}
