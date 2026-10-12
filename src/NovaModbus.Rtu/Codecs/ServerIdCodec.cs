namespace NovaModbus.Rtu.Codecs;

/// <summary>FC11 Report Server ID. Request is empty; response is [byte count(1)][server ID...][run indicator(1)][additional...].</summary>
public static class ServerIdCodec
{
    /// <summary>Encode the empty request body.</summary>
    /// <returns>An empty byte array, as the FC11 request carries no data.</returns>
    public static byte[] EncodeRequest() => [];

    /// <summary>Encode response body.</summary>
    /// <param name="serverId">Server identifier bytes (any length); total payload must fit in 255 bytes.</param>
    /// <param name="running">Run indicator; <c>true</c> encodes 0xFF (running), <c>false</c> encodes 0x00 (stopped).</param>
    /// <param name="additional">Additional device-specific bytes appended after the run indicator.</param>
    /// <returns>The encoded body: [byte count(1)][server ID...][run indicator(1)][additional...].</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when server ID plus run indicator plus additional data exceeds 255 bytes.</exception>
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
    /// <param name="body">Response body; at least 3 bytes with <c>body[0]</c> equal to <c>body.Length - 1</c>.</param>
    /// <param name="serverId">Receives the server identifier bytes (all bytes between the count and the run indicator).</param>
    /// <param name="running">Receives <c>true</c> when the run indicator is 0xFF, otherwise <c>false</c>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is shorter than 3 bytes or its byte count mismatches.</exception>
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
    /// <param name="body">Response body; at least <c>2 + serverIdLength</c> bytes with <c>body[0]</c> equal to <c>body.Length - 1</c>.</param>
    /// <param name="serverIdLength">Length of the server identifier in bytes (0 or greater; must fit within <paramref name="body"/>).</param>
    /// <param name="serverId">Receives the server identifier bytes.</param>
    /// <param name="running">Receives <c>true</c> when the run indicator is 0xFF, otherwise <c>false</c>.</param>
    /// <param name="additional">Receives the trailing device-specific bytes after the run indicator.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="serverIdLength"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is too short or its byte count mismatches.</exception>
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
