using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Enums;
using NovaModbus.Rtu.Errors;
using NovaModbus.Rtu.Tests.Testing;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class SlaveDispatchTests
{
    private static ModbusRtuSlave CreateSlave(byte slaveId = 1)
    {
        FakeSerialTransport transport = new();
        ModbusRtuSlave slave = new(transport);
        slave.Engine.AddSlave(slaveId);
        return slave;
    }

    private static byte[] Request(byte slaveId, byte function, byte[] body) =>
        new ModbusFrame(slaveId, function, body).Encode();

    private static ModbusFrame DecodeOk(byte[]? response, byte expectedFunction)
    {
        Assert.IsNotNull(response);
        Assert.IsTrue(ModbusFrame.TryDecode(response, out ModbusFrame? frame));
        Assert.IsNotNull(frame);
        Assert.IsFalse(frame.IsException);
        Assert.AreEqual(expectedFunction, frame.Function);
        return frame;
    }

    private static ModbusExceptionCode DecodeException(byte[]? response, byte baseFunction)
    {
        Assert.IsNotNull(response);
        Assert.IsTrue(ModbusFrame.TryDecode(response, out ModbusFrame? frame));
        Assert.IsNotNull(frame);
        Assert.IsTrue(frame.IsException);
        Assert.AreEqual((byte)(baseFunction | 0x80), frame.Function);
        return ExceptionCodec.Decode(frame.Data);
    }

    [TestMethod]
    public void ReadHoldingReturnsSeededValues()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        store.WriteHolding(0, [0x022B, 0x0000, 0x0064]);

        byte[]? response = slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 3)));
        ModbusFrame frame = DecodeOk(response, 0x03);
        CollectionAssert.AreEqual(new ushort[] { 0x022B, 0x0000, 0x0064 }, ReadRegistersCodec.DecodeResponse(frame.Data));
    }

    [TestMethod]
    public void ReadCoilsReturnsSeededValues()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        bool[] seed = [true, false, true];
        store.WriteCoils(4, seed);

        byte[]? response = slave.HandleRequest(Request(1, 0x01, ReadBitsCodec.EncodeRequest(4, (ushort)seed.Length)));
        ModbusFrame frame = DecodeOk(response, 0x01);
        CollectionAssert.AreEqual(seed, ReadBitsCodec.DecodeResponse(frame.Data, seed.Length));
    }

    [TestMethod]
    public void WriteSingleCoilThenReadBack()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[]? write = slave.HandleRequest(Request(1, 0x05, WriteSingleCoilCodec.Encode(7, true)));
        DecodeOk(write, 0x05);

        byte[]? read = slave.HandleRequest(Request(1, 0x01, ReadBitsCodec.EncodeRequest(7, 1)));
        ModbusFrame frame = DecodeOk(read, 0x01);
        bool[] expected = [true];
        CollectionAssert.AreEqual(expected, ReadBitsCodec.DecodeResponse(frame.Data, 1));
    }

    [TestMethod]
    public void WriteMultipleRegistersThenReadBack()
    {
        ModbusRtuSlave slave = CreateSlave();
        ushort[] values = [0x000A, 0x0102];
        byte[]? write = slave.HandleRequest(Request(1, 0x10, WriteMultipleRegistersCodec.EncodeRequest(3, values)));
        DecodeOk(write, 0x10);

        byte[]? read = slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(3, (ushort)values.Length)));
        ModbusFrame frame = DecodeOk(read, 0x03);
        CollectionAssert.AreEqual(values, ReadRegistersCodec.DecodeResponse(frame.Data));
    }

    [TestMethod]
    public void WriteMultipleCoilsThenReadBack()
    {
        ModbusRtuSlave slave = CreateSlave();
        bool[] values = [true, true, false, true];
        byte[]? write = slave.HandleRequest(Request(1, 0x0F, WriteMultipleCoilsCodec.EncodeRequest(0, values)));
        DecodeOk(write, 0x0F);

        byte[]? read = slave.HandleRequest(Request(1, 0x01, ReadBitsCodec.EncodeRequest(0, (ushort)values.Length)));
        ModbusFrame frame = DecodeOk(read, 0x01);
        CollectionAssert.AreEqual(values, ReadBitsCodec.DecodeResponse(frame.Data, values.Length));
    }

    [TestMethod]
    public void OutOfRangeReadGivesIllegalDataAddress()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[]? response = slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0xFFFF, 2)));
        Assert.AreEqual(ModbusExceptionCode.IllegalDataAddress, DecodeException(response, 0x03));
    }

    [TestMethod]
    public void ZeroCountReadGivesIllegalDataValue()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[] raw = [0x01, 0x03, 0x00, 0x00, 0x00, 0x00];
        ushort crc = Crc16.Compute(raw);
        byte[] wire = [.. raw, (byte)(crc & 0xFF), (byte)(crc >> 8)];

        byte[]? response = slave.HandleRequest(wire);
        Assert.AreEqual(ModbusExceptionCode.IllegalDataValue, DecodeException(response, 0x03));
    }

    [TestMethod]
    public void UnknownFunctionGivesIllegalFunction()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[]? response = slave.HandleRequest(Request(1, 0x2B, [0x0E, 0x01, 0x00]));
        Assert.AreEqual(ModbusExceptionCode.IllegalFunction, DecodeException(response, 0x2B));
    }

    [TestMethod]
    public void UnknownSlaveAndBroadcastGetNoResponse()
    {
        ModbusRtuSlave slave = CreateSlave();
        Assert.IsNull(slave.HandleRequest(Request(7, 0x03, ReadRegistersCodec.EncodeRequest(0, 1))));
        Assert.IsNull(slave.HandleRequest(Request(0, 0x03, ReadRegistersCodec.EncodeRequest(0, 1))));
    }

    [TestMethod]
    public void CorruptFrameDefaultNoResponse()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[] wire = Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1));
        wire[^1] ^= 0xFF;
        Assert.IsNull(slave.HandleRequest(wire));
    }

    [TestMethod]
    public void CorruptFrameStrictModeThrows()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.IgnoreCrcErrors = false;
        byte[] wire = Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1));
        wire[^1] ^= 0xFF;
        Assert.ThrowsExactly<CrcMismatchException>(() => slave.HandleRequest(wire));
    }

    [TestMethod]
    public void BusySimulationGivesBusyException()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.SimulateBusy = true;
        byte[]? response = slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)));
        Assert.AreEqual(ModbusExceptionCode.SlaveDeviceBusy, DecodeException(response, 0x03));
    }

    [TestMethod]
    public void MultiIdRoutingIsIndependent()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.AddSlave(2);
        slave.Engine.TryGetStore(1, out ModbusDataStore? s1);
        slave.Engine.TryGetStore(2, out ModbusDataStore? s2);
        Assert.IsNotNull(s1);
        Assert.IsNotNull(s2);
        s1.WriteHolding(0, 111);
        s2.WriteHolding(0, 222);

        byte[]? r1 = slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)));
        byte[]? r2 = slave.HandleRequest(Request(2, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)));
        Assert.AreEqual((ushort)111, ReadRegistersCodec.DecodeResponse(DecodeOk(r1, 0x03).Data)[0]);
        Assert.AreEqual((ushort)222, ReadRegistersCodec.DecodeResponse(DecodeOk(r2, 0x03).Data)[0]);
        Assert.AreEqual(1, slave.Engine.GetCount(1));
        Assert.AreEqual(1, slave.Engine.GetCount(2));
    }

    [TestMethod]
    public void DiagnosticsNakCountReturnsZero()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[]? response = slave.HandleRequest(Request(
            1, 0x08, DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ReturnSlaveNakCount, [0x00, 0x00])));
        ModbusFrame frame = DecodeOk(response, 0x08);
        DiagnosticsCodec.Decode(frame.Data, out ushort sub, out byte[] data);
        Assert.AreEqual(DiagnosticsCodec.Subfunctions.ReturnSlaveNakCount, sub);
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00 }, data);
    }

    [TestMethod]
    public void ClearCountersResetsNak()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        store.NoteNak();
        Assert.AreEqual(1, store.NakCount);

        byte[]? response = slave.HandleRequest(Request(
            1, 0x08, DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ClearCounters, [0x00, 0x00])));
        Assert.IsNotNull(response);
        Assert.AreEqual(0, store.NakCount);
    }

    [TestMethod]
    public void ClearOverrunCounterEchoesAndClears()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.NoteOverrun();
        slave.Engine.NoteOverrun();
        Assert.AreEqual(2L, slave.Engine.OverrunCount);

        byte[]? response = slave.HandleRequest(Request(
            1, 0x08, DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ClearOverrunCounter, [0x00, 0x00])));
        ModbusFrame frame = DecodeOk(response, 0x08);
        DiagnosticsCodec.Decode(frame.Data, out ushort sub, out byte[] data);
        Assert.AreEqual(DiagnosticsCodec.Subfunctions.ClearOverrunCounter, sub);
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00 }, data);
        Assert.AreEqual(0L, slave.Engine.OverrunCount);
    }

    [TestMethod]
    public void ListenOnlySilencesUntilRestart()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);

        byte[]? enter = slave.HandleRequest(Request(
            1, 0x08, DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ForceListenOnlyMode, [0x00, 0x00])));
        Assert.IsNull(enter);
        Assert.IsTrue(store.ListenOnly);

        // Normal requests are monitored only: no response, no further counting
        // (the 0x04 entry request itself was processed normally: count 1).
        Assert.IsNull(slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1))));
        Assert.AreEqual(1, store.SlaveMessageCount);

        // Malformed restart stays silent and stays listen-only.
        Assert.IsNull(slave.HandleRequest(Request(
            1, 0x08, DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.RestartCommunicationsOption, [0x12, 0x34]))));
        Assert.IsTrue(store.ListenOnly);

        // Valid restart exits listen-only without responding.
        Assert.IsNull(slave.HandleRequest(Request(
            1, 0x08, DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.RestartCommunicationsOption, [0x00, 0x00]))));
        Assert.IsFalse(store.ListenOnly);

        // Line serves again.
        Assert.IsNotNull(slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1))));
    }

    [TestMethod]
    public void LineErrorFeedsOverrunAndCommCounters()
    {
        FakeSerialTransport transport = new();
        ModbusRtuSlave slave = new(transport);
        slave.Engine.AddSlave(1);

        transport.SimulateLineError(SerialLineError.Overrun);
        Assert.AreEqual(1L, slave.Engine.OverrunCount);
        Assert.AreEqual(1L, slave.Engine.CharErrorCount);

        // Bus communication-error count includes character-level errors.
        byte[]? response = slave.HandleRequest(Request(
            1, 0x08, DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ReturnBusCommErrorCount, [0x00, 0x00])));
        ModbusFrame frame = DecodeOk(response, 0x08);
        DiagnosticsCodec.Decode(frame.Data, out _, out byte[] data);
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x01 }, data);
    }
}
