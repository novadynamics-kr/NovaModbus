using NovaModbus.Rtu;

namespace NovaModbus.Client.Services;

/// <summary>Shared value formatting for read results.</summary>
public static class ValueFormatter
{
    /// <summary>Bits as "1 0 1".</summary>
    public static string FormatBits(bool[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return string.Join(' ', values.Select(b => b ? "1" : "0"));
    }

    /// <summary>Registers as "[i] 0xXXXX (n)" lines.</summary>
    public static string FormatRegisters(ushort[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return string.Join(Environment.NewLine, values.Select((v, i) => $"[{i}] 0x{v:X4} ({v})"));
    }

    /// <summary>Device ID bytes as ASCII text, or hex when not printable.</summary>
    public static string FormatDeviceId(byte[] id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return id.All(b => b is >= 0x20 and < 0x7F)
            ? System.Text.Encoding.ASCII.GetString(id)
            : Hex.Format(id);
    }
}
