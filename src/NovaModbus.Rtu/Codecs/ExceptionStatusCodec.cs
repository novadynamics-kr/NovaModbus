namespace NovaModbus.Rtu.Codecs;

/// <summary>FC07 Read Exception Status. Request is empty; response is one status byte.</summary>
public static class ExceptionStatusCodec
{
    /// <summary>Encode the empty request body.</summary>
    public static byte[] EncodeRequest() => [];

    /// <summary>Decode the one-byte response.</summary>
    public static byte DecodeResponse(ReadOnlySpan<byte> body)
    {
        if (body.Length != 1)
        {
            throw new ArgumentException("FC07 response must be 1 byte.", nameof(body));
        }

        return body[0];
    }
}
