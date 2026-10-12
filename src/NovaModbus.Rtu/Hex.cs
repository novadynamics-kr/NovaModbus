namespace NovaModbus.Rtu;

/// <summary>Uppercase hex formatting for raw frame logs ("01 03 ...").</summary>
public static class Hex
{
    /// <summary>Format bytes as uppercase hex separated by spaces.</summary>
    /// <param name="data">Bytes to format.</param>
    /// <returns>Empty string for empty input; otherwise "XX XX ..." (e.g. "01 03").</returns>
    public static string Format(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(' ', data.ToArray().Select(b => b.ToString("X2", null)));
    }
}
