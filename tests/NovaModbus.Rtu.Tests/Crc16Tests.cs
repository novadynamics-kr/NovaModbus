using NovaModbus.Rtu;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class Crc16Tests
{
    [TestMethod]
    public void KnownVectorReadHoldingRequestMatches()
    {
        // 01 03 00 00 00 0A -> CRC C5 CD (Lo first).
        byte[] req = [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A];
        ushort crc = Crc16.Compute(req);
        Assert.AreEqual(0xCDC5, crc);
    }

    [TestMethod]
    public void FrameEncodeAppendsCrcLoFirst()
    {
        ModbusFrame frame = new(0x01, 0x03, [0x00, 0x00, 0x00, 0x0A]);
        byte[] wire = frame.Encode();
        Assert.HasCount(8, wire);
        Assert.AreEqual(0xC5, wire[6]);
        Assert.AreEqual(0xCD, wire[7]);
    }

    [TestMethod]
    public void FrameRoundTripDecodeVerifiesCrc()
    {
        ModbusFrame frame = new(0x01, 0x03, [0x00, 0x00, 0x00, 0x0A]);
        byte[] wire = frame.Encode();
        Assert.IsTrue(ModbusFrame.TryDecode(wire, out ModbusFrame? decoded));
        Assert.IsNotNull(decoded);
        Assert.AreEqual((byte)0x01, decoded.SlaveId);
        Assert.AreEqual((byte)0x03, decoded.Function);
    }

    [TestMethod]
    public void CorruptCrcDecodeFails()
    {
        ModbusFrame frame = new(0x01, 0x03, [0x00, 0x00, 0x00, 0x0A]);
        byte[] wire = frame.Encode();
        wire[^1] ^= 0xFF;
        Assert.IsFalse(ModbusFrame.TryDecode(wire, out _));
    }
}
