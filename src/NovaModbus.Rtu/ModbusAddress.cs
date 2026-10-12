namespace NovaModbus.Rtu;

/// <summary>Modbus address text parsing (decimal or 0x hex).</summary>
public static class ModbusAddress
{
    /// <summary>Parse an address (e.g. "10" or "0x000A").</summary>
    /// <param name="text">Decimal digits or a "0x"-prefixed hex value; surrounding whitespace is ignored.</param>
    /// <returns>Parsed address in 0-65535.</returns>
    /// <exception cref="ArgumentException">Thrown for null/blank text or out-of-range values.</exception>
    public static ushort Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        string trimmed = text.Trim();
        try
        {
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (trimmed.Length == 2)
                {
                    throw new ArgumentException($"Invalid address: '{text}'.");
                }

                return Convert.ToUInt16(trimmed[2..], 16);
            }

            return ushort.Parse(trimmed, null);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new ArgumentException($"Invalid address: '{text}'.", ex);
        }
    }

    /// <summary>Parse and check against a capacity (exclusive upper bound).</summary>
    /// <param name="text">Decimal digits or a "0x"-prefixed hex value.</param>
    /// <param name="capacity">Exclusive upper bound; the parsed address must be below it.</param>
    /// <returns>Parsed address in 0..capacity-1.</returns>
    /// <exception cref="ArgumentException">Thrown for invalid text or addresses at/above <paramref name="capacity"/>.</exception>
    public static ushort Parse(string text, int capacity)
    {
        ushort address = Parse(text);
        if (address >= capacity)
        {
            throw new ArgumentException($"Address out of range: '{text}'.");
        }

        return address;
    }
}
