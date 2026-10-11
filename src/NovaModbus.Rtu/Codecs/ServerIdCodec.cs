namespace NovaModbus.Rtu.Codecs;

/// <summary>FC11 Report Server ID. Request is empty; response is [byte count(1)][server ID...][run indicator(1)][additional...].</summary>
public static class ServerIdCodec
{
    /// <summary>Encode the empty request body.</summary>
    public static byte[] EncodeRequest() => [];

    /// <summary>Encode response body.</summary>
    public static byte[] EncodeResponse(ReadOnlySpan<byte> serverId, bool running, ReadOnlySpan<byte> additional)
    {
        if (serverId.Length + 1 + additional.Length > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(serverId), "Server ID payload too long.");
        }

        byte[] body = new byte[1 + serverId.Length + 1 + additional.Length];
        body[0] = (byte)(serverId.Length + 1 + additional.Length);
        serverId.CopyTo(body.AsSpan(1, serverId.Length));
        body[1 + serverId.Length] = running ? (byte)0xFF : (byte)0x00;
        additional.CopyTo(body.AsSpan(2 + serverId.Length));
        return body;
    }

    /// <summary>
    /// Decode assuming no additional data (server ID = all but the last byte).
    /// Use the <paramref name="serverIdLength"/> overload when additional data exists.
    /// </summary>
    public static void DecodeResponse(ReadOnlySpan<byte> body, out byte[] serverId, out bool running)
    {
        if (body.Length < 3 || body[0] != body.Length - 1)
        {
            throw new ArgumentException("FC11 byte-count mismatch.", nameof(body));
        }

        serverId = body.Slice(1, body.Length - 2).ToArray();
        running = body[^1] == 0xFF;
    }

    /// <summary>Decode with a known server ID length, splitting off additional data.</summary>
    public static void DecodeResponse(ReadOnlySpan<byte> body, int serverIdLength, out byte[] serverId, out bool running, out byte[] additional)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(serverIdLength, 0);
        if (body.Length < 2 + serverIdLength || body[0] != body.Length - 1)
        {
            throw new ArgumentException("FC11 byte-count mismatch.", nameof(body));
        }

        serverId = body.Slice(1, serverIdLength).ToArray();
        running = body[1 + serverIdLength] == 0xFF;
        additional = body.Slice(2 + serverIdLength).ToArray();
    }
}
