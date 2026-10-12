namespace NovaModbus.Rtu.Codecs;

/// <summary>FC07 Read Exception Status. Request is empty; response is one status byte.</summary>
public static class ExceptionStatusCodec
{
    /// <summary>Encode the empty request body.</summary>
    /// <returns>An empty byte array, as the FC07 request carries no data.</returns>
    public static byte[] EncodeRequest() => [];

    /// <summary>Decode the one-byte response.</summary>
    /// <param name="body">Response body; must be exactly 1 byte holding the exception-status outputs.</param>
    /// <returns>The exception-status byte sent by the server.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is not exactly 1 byte.</exception>
    public static byte DecodeResponse(ReadOnlySpan<byte> body)
    {
        if (body.Length != 1)
        {
            throw new ArgumentException("FC07 response must be 1 byte.", nameof(body));
        }

        return body[0];
    }
}
