using NovaModbus.Rtu.Enums;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu;

/// <summary>RTU ADU: [SlaveId(1)][Func(1)][Data(N)][CRC Lo][CRC Hi].</summary>
public sealed class ModbusFrame
{
    /// <summary>Gets the slave address (1-247, 0 = broadcast).</summary>
    public byte SlaveId { get; }

    /// <summary>Gets the function code.</summary>
    public byte Function { get; }

    /// <summary>Gets the PDU payload following the function byte.</summary>
    public byte[] Data { get; }

    /// <summary>Initializes a new frame.</summary>
    public ModbusFrame(byte slaveId, byte function, byte[] data)
    {
        SlaveId = slaveId;
        Function = function;
        Data = data;
    }

    /// <summary>Encode to wire bytes including CRC.</summary>
    public byte[] Encode()
    {
        byte[] frame = new byte[2 + Data.Length + 2];
        frame[0] = SlaveId;
        frame[1] = Function;
        Data.CopyTo(frame, 2);
        Crc16.AppendCrc(frame.AsSpan(0, 2 + Data.Length), frame.AsSpan(2 + Data.Length, 2));
        return frame;
    }

    /// <summary>Try decode and verify CRC. Returns false on length/CRC error.</summary>
    public static bool TryDecode(ReadOnlySpan<byte> raw, out ModbusFrame? frame)
    {
        frame = null;
        if (raw.Length < 4 || raw.Length > RtuFramer.MaxFrameSize || !Crc16.Verify(raw))
        {
            return false;
        }

        frame = new ModbusFrame(raw[0], raw[1], raw.Slice(2, raw.Length - 4).ToArray());
        return true;
    }

    /// <summary>True when this frame is a Modbus exception (func MSB set).</summary>
    public bool IsException => (Function & 0x80) != 0;

    /// <summary>Base function without the exception flag.</summary>
    public ModbusFunction BaseFunction => (ModbusFunction)(Function & 0x7F);
}
