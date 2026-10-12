using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC0B Get Comm Event Counter. Request is empty; response is [status BE(2)][event count BE(2)].</summary>
public static class CommEventCounterCodec
{
    /// <summary>Encode the empty request body.</summary>
    /// <returns>An empty byte array, as the FC0B request carries no data.</returns>
    public static byte[] EncodeRequest() => [];

    /// <summary>Encode response body.</summary>
    /// <param name="status">Status word (0x0000-0xFFFF; 0xFFFF means the server is in listen-only mode).</param>
    /// <param name="eventCount">Communication event counter (0x0000-0xFFFF).</param>
    /// <returns>A 4-byte body: [status BE(2)][event count BE(2)].</returns>
    public static byte[] EncodeResponse(ushort status, ushort eventCount)
    {
        byte[] body = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), status);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), eventCount);
        return body;
    }

    /// <summary>Decode response body.</summary>
    /// <param name="body">Response body; must be exactly 4 bytes: [status BE(2)][event count BE(2)].</param>
    /// <param name="status">Receives the status word.</param>
    /// <param name="eventCount">Receives the communication event counter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is not exactly 4 bytes.</exception>
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
