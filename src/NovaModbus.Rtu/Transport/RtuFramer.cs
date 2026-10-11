namespace NovaModbus.Rtu.Transport;

/// <summary>RTU timing helper: 3.5-char silent interval from baud rate.</summary>
public static class RtuFramer
{
    private const int BitsPerChar = 11; // start + 8 data + parity + stop (worst case)

    /// <summary>Compute 3.5-char silence for the given baud rate (min 2ms floor).</summary>
    public static TimeSpan SilentInterval(int baudRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baudRate, 0);
        double ms = (BitsPerChar * 3.5 / baudRate) * 1000.0;
        return TimeSpan.FromMilliseconds(Math.Max(ms, 2.0));
    }
}
