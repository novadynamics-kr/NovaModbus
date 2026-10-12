using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC0C Get Comm Event Log. Request is empty; response is [status(2)][event count(2)][message count(2)][events 0-64].</summary>
public static class CommEventLogCodec
{
    /// <summary>Encode the empty request body.</summary>
    /// <returns>An empty byte array, as the FC0C request carries no data.</returns>
    public static byte[] EncodeRequest() => [];

    /// <summary>Encode response body.</summary>
    /// <param name="status">Status word (0x0000-0xFFFF; 0xFFFF means the server is busy).</param>
    /// <param name="eventCount">Communication event counter (0x0000-0xFFFF).</param>
    /// <param name="messageCount">Message counter (0x0000-0xFFFF).</param>
    /// <param name="events">Event bytes (0-64 bytes); copied verbatim after the three counters.</param>
    /// <returns>The encoded body: [status(2)][event count(2)][message count(2)][events].</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="events"/> exceeds 64 bytes.</exception>
    public static byte[] EncodeResponse(ushort status, ushort eventCount, ushort messageCount, ReadOnlySpan<byte> events)
    {
        if (events.Length > ModbusLimits.MaxEventLogBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(events), $"At most {ModbusLimits.MaxEventLogBytes} event bytes.");
        }

        byte[] body = new byte[6 + events.Length];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), status);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2, 2), eventCount);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(4, 2), messageCount);
        events.CopyTo(body.AsSpan(6));
        return body;
    }

    /// <summary>Decode response body.</summary>
    /// <param name="body">Response body; 6-70 bytes: [status(2)][event count(2)][message count(2)][events 0-64].</param>
    /// <param name="status">Receives the status word.</param>
    /// <param name="eventCount">Receives the communication event counter.</param>
    /// <param name="messageCount">Receives the message counter.</param>
    /// <param name="events">Receives a copy of the trailing event bytes (may be empty).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is shorter than 6 bytes or longer than 70 bytes.</exception>
    public static void DecodeResponse(ReadOnlySpan<byte> body, out ushort status, out ushort eventCount, out ushort messageCount, out byte[] events)
    {
        if (body.Length < 6 || body.Length > 6 + ModbusLimits.MaxEventLogBytes)
        {
            throw new ArgumentException("FC0C response length out of range.", nameof(body));
        }

        status = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        eventCount = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(2, 2));
        messageCount = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(4, 2));
        events = body[6..].ToArray();
    }
}
