using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC0B Get Comm Event Counter. Request is empty; response is [status BE(2)][event count BE(2)].</summary>
public static class CommEventCounterCodec
{
    /// <summary>Encode the empty request body.</summary>
    public static byte[] EncodeRequest() => [];

    /// <summary>Encode response body.</summary>
    public static byte[] EncodeResponse(ushort status, ushort eventCount)
    {
        byte[] body = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), status);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), eventCount);
        return body;
    }

    /// <summary>Decode response body.</summary>
    public static void DecodeResponse(ReadOnlySpan<byte> body, out ushort status, out ushort eventCount)
    {
        if (body.Length != 4)
        {
            throw new ArgumentException("FC0B response must be 4 bytes.", nameof(body));
        }

        status = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        eventCount = BinaryPrimitives.ReadUInt16BigEndian(body[2..]);
    }
}
