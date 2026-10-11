using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC0C Get Comm Event Log. Request is empty; response is [status(2)][event count(2)][message count(2)][events 0-64].</summary>
public static class CommEventLogCodec
{
    /// <summary>Encode the empty request body.</summary>
    public static byte[] EncodeRequest() => [];

    /// <summary>Encode response body.</summary>
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
