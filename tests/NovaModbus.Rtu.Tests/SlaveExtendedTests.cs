using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Enums;
using NovaModbus.Rtu.Tests.Testing;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class SlaveExtendedTests
{
    private static ModbusRtuSlave CreateSlave()
    {
        FakeSerialTransport transport = new();
        ModbusRtuSlave slave = new(transport);
        slave.Engine.AddSlave(1);
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

    private static ModbusExceptionCode DecodeException(byte[]? response)
    {
        Assert.IsNotNull(response);
        Assert.IsTrue(ModbusFrame.TryDecode(response, out ModbusFrame? frame));
        Assert.IsNotNull(frame);
        Assert.IsTrue(frame.IsException);
        return ExceptionCodec.Decode(frame.Data);
    }

    [TestMethod]
    public void ExceptionStatusReturnsConfiguredByte()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        store.ExceptionStatus = 0x5A;

        byte[]? response = slave.HandleRequest(Request(1, 0x07, ExceptionStatusCodec.EncodeRequest()));
        Assert.AreEqual((byte)0x5A, ExceptionStatusCodec.DecodeResponse(DecodeOk(response, 0x07).Data));
    }

    [TestMethod]
    public void DiagnosticsQueryDataEchoes()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[] payload = [0x12, 0x34];
        byte[]? response = slave.HandleRequest(Request(1, 0x08, DiagnosticsCodec.Encode(0x0000, payload)));
        DiagnosticsCodec.Decode(DecodeOk(response, 0x08).Data, out ushort sub, out byte[] data);
        Assert.AreEqual((ushort)0x0000, sub);
        CollectionAssert.AreEqual(payload, data);
    }

    [TestMethod]
    public void DiagnosticsCountersTrackRequests()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)));

        byte[]? response = slave.HandleRequest(Request(1, 0x08,
            DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ReturnBusMessageCount, [])));
        DiagnosticsCodec.Decode(DecodeOk(response, 0x08).Data, out _, out byte[] data);
        Assert.AreEqual((ushort)2, System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(data));
        Assert.AreEqual(2, slave.Engine.BusMessageCount);
    }

    [TestMethod]
    public void DiagnosticsClearCountersResets()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)));
        slave.HandleRequest(Request(1, 0x08,
            DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ClearCounters, [0x00, 0x00])));

        byte[]? response = slave.HandleRequest(Request(1, 0x08,
            DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ReturnSlaveMessageCount, [])));
        DiagnosticsCodec.Decode(DecodeOk(response, 0x08).Data, out _, out byte[] data);
        Assert.AreEqual((ushort)1, System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(data));
    }

    [TestMethod]
    public void DiagnosticsUnknownSubGivesIllegalFunction()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[]? response = slave.HandleRequest(Request(1, 0x08, DiagnosticsCodec.Encode(0x00FF, [])));
        Assert.AreEqual(ModbusExceptionCode.IllegalFunction, DecodeException(response));
    }

    [TestMethod]
    public void ForceListenOnlyGetsNoResponse()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[]? response = slave.HandleRequest(Request(1, 0x08,
            DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ForceListenOnlyMode, [0x00, 0x00])));
        Assert.IsNull(response);
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        Assert.AreEqual(1, store.NoResponseCount);
    }

    [TestMethod]
    public void CorruptFrameCountsBusCrcError()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[] wire = Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1));
        wire[^1] ^= 0xFF;
        Assert.IsNull(slave.HandleRequest(wire));
        Assert.AreEqual(1, slave.Engine.BusCrcErrorCount);
        Assert.AreEqual(0, slave.Engine.BusMessageCount);
    }

    [TestMethod]
    public void CommEventCounterAndLog()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)));

        // FC0B polls do not increment the event counter (spec §6.9).
        byte[]? counter = slave.HandleRequest(Request(1, 0x0B, CommEventCounterCodec.EncodeRequest()));
        CommEventCounterCodec.DecodeResponse(DecodeOk(counter, 0x0B).Data, out ushort status, out ushort events);
        Assert.AreEqual((ushort)0x0000, status);
        Assert.AreEqual((ushort)1, events);

        // Message count is bus-level (== FC08 sub 0x0B): 3 valid frames so far.
        // Log is newest-first: [FC0C-rx, FC0B-tx, FC0B-rx, FC03-tx, FC03-rx].
        byte[]? log = slave.HandleRequest(Request(1, 0x0C, CommEventLogCodec.EncodeRequest()));
        CommEventLogCodec.DecodeResponse(DecodeOk(log, 0x0C).Data, out _, out _, out ushort messages, out byte[] logged);
        Assert.AreEqual((ushort)3, messages);
        CollectionAssert.AreEqual(new byte[] { 0x80, 0x40, 0x80, 0x40, 0x80 }, logged);
    }

    [TestMethod]
    public void EventCountExcludesPollsAndExceptions()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);

        slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)));
        Assert.AreEqual(1, store.EventCount);

        // Polls (FC07) and counter fetches (FC0B/FC0C) never increment.
        slave.HandleRequest(Request(1, 0x07, ExceptionStatusCodec.EncodeRequest()));
        slave.HandleRequest(Request(1, 0x0B, CommEventCounterCodec.EncodeRequest()));
        slave.HandleRequest(Request(1, 0x0C, CommEventLogCodec.EncodeRequest()));
        Assert.AreEqual(1, store.EventCount);

        // Exception responses never increment.
        slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0xFFFF, 2)));
        Assert.AreEqual(1, store.EventCount);

        slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)));
        Assert.AreEqual(2, store.EventCount);
    }

    [TestMethod]
    public void EventLogBytesFollowSpecLayoutNewestFirst()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);

        slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)));
        CollectionAssert.AreEqual(new byte[] { 0x40, 0x80 }, store.ReadEventLog());

        // Illegal address → exception 0x02 → send byte 0x41, no count increment.
        slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0xFFFF, 2)));
        CollectionAssert.AreEqual(new byte[] { 0x41, 0x80, 0x40, 0x80 }, store.ReadEventLog());
        Assert.AreEqual(1, store.EventCount);
    }

    [TestMethod]
    public void RestartResetsEventCounter()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);

        slave.HandleRequest(Request(1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)));
        Assert.AreEqual(1, store.EventCount);

        byte[]? response = slave.HandleRequest(Request(
            1, 0x08, DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.RestartCommunicationsOption, [0x00, 0x00])));
        Assert.IsNotNull(response);
        Assert.AreEqual(0, store.EventCount);
        // Restart logs its dedicated byte (no generic send event).
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x80, 0x40, 0x80 }, store.ReadEventLog());
    }

    [TestMethod]
    public void ListenOnlyEnterLogsDedicatedByte()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);

        Assert.IsNull(slave.HandleRequest(Request(
            1, 0x08, DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ForceListenOnlyMode, [0x00, 0x00]))));
        CollectionAssert.AreEqual(new byte[] { 0x20, 0x80 }, store.ReadEventLog());
    }

    [TestMethod]
    public void ReportServerIdDefault()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[]? response = slave.HandleRequest(Request(1, 0x11, ServerIdCodec.EncodeRequest()));
        ServerIdCodec.DecodeResponse(DecodeOk(response, 0x11).Data, out byte[] id, out bool running);
        Assert.IsTrue(running);
        Assert.AreEqual("NovaModbus", System.Text.Encoding.ASCII.GetString(id));
    }

    [TestMethod]
    public void FileWriteThenRead()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        store.EnsureFile(4, 10);

        FileSubRequest[] subs = [new(4, 7, 2)];
        ushort[][] datas = [[0x0006, 0x0009]];
        byte[]? write = slave.HandleRequest(Request(1, 0x15, FileRecordCodec.EncodeWriteRequest(subs, datas)));
        DecodeOk(write, 0x15);

        byte[]? read = slave.HandleRequest(Request(1, 0x14, FileRecordCodec.EncodeReadRequest(subs)));
        ushort[][] records = FileRecordCodec.DecodeReadResponse(DecodeOk(read, 0x14).Data);
        Assert.HasCount(1, records);
        CollectionAssert.AreEqual(datas[0], records[0]);
    }

    [TestMethod]
    public void FileUnknownGivesIllegalDataAddress()
    {
        ModbusRtuSlave slave = CreateSlave();
        FileSubRequest[] subs = [new(9, 0, 1)];
        byte[]? response = slave.HandleRequest(Request(1, 0x14, FileRecordCodec.EncodeReadRequest(subs)));
        Assert.AreEqual(ModbusExceptionCode.IllegalDataAddress, DecodeException(response));
    }

    [TestMethod]
    public void MaskWriteAppliesAlgorithm()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        store.WriteHolding(4, 0x0012);

        byte[]? response = slave.HandleRequest(Request(1, 0x16, MaskWriteCodec.Encode(4, 0x00F2, 0x0025)));
        DecodeOk(response, 0x16);
        Assert.AreEqual((ushort)0x0017, store.ReadHolding(4, 1)[0]);
    }

    [TestMethod]
    public void ReadWriteMultipleWritesThenReads()
    {
        ModbusRtuSlave slave = CreateSlave();
        ushort[] writeValues = [0x00FF, 0x00EE];
        byte[]? response = slave.HandleRequest(Request(1, 0x17,
            ReadWriteMultipleCodec.EncodeRequest(10, 2, 10, writeValues)));
        CollectionAssert.AreEqual(writeValues, ReadWriteMultipleCodec.DecodeResponse(DecodeOk(response, 0x17).Data));

        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        CollectionAssert.AreEqual(writeValues, store.ReadHolding(10, 2));
    }

    [TestMethod]
    public void FifoReadReturnsSeededValues()
    {
        ModbusRtuSlave slave = CreateSlave();
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        store.EnsureFifo(4);
        store.EnqueueFifo(4, 0x01AE);
        store.EnqueueFifo(4, 0x0124);

        byte[]? response = slave.HandleRequest(Request(1, 0x18, FifoQueueCodec.EncodeRequest(4)));
        CollectionAssert.AreEqual(new ushort[] { 0x01AE, 0x0124 }, FifoQueueCodec.DecodeResponse(DecodeOk(response, 0x18).Data));
    }

    [TestMethod]
    public void FifoUnknownGivesIllegalDataAddress()
    {
        ModbusRtuSlave slave = CreateSlave();
        byte[]? response = slave.HandleRequest(Request(1, 0x18, FifoQueueCodec.EncodeRequest(4)));
        Assert.AreEqual(ModbusExceptionCode.IllegalDataAddress, DecodeException(response));
    }
}
