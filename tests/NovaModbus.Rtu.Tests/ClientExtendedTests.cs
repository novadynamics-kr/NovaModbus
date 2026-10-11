using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Tests.Testing;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class ClientExtendedTests
{
    private static byte[] Wire(byte slaveId, byte function, byte[] body) =>
        new ModbusFrame(slaveId, function, body).Encode();

    [TestMethod]
    public async Task ExceptionStatusRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        transport.EnqueueInbound(Wire(1, 0x07, [0x5A]));

        Assert.AreEqual((byte)0x5A, await client.ReadExceptionStatusAsync(1));
        CollectionAssert.AreEqual(Wire(1, 0x07, ExceptionStatusCodec.EncodeRequest()), transport.TakeWritten());
    }

    [TestMethod]
    public async Task DiagnosticsEchoRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        byte[] payload = [0x12, 0x34];
        transport.EnqueueInbound(Wire(1, 0x08, DiagnosticsCodec.Encode(0x0000, payload)));

        CollectionAssert.AreEqual(payload, await client.DiagnosticsAsync(1, 0x0000, payload));
    }

    [TestMethod]
    public async Task CommEventCounterRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        transport.EnqueueInbound(Wire(1, 0x0B, CommEventCounterCodec.EncodeResponse(0x0000, 0x0064)));

        (ushort status, ushort events) = await client.GetCommEventCounterAsync(1);
        Assert.AreEqual((ushort)0x0000, status);
        Assert.AreEqual((ushort)0x0064, events);
    }

    [TestMethod]
    public async Task CommEventLogRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        byte[] events = [0x70, 0x00];
        transport.EnqueueInbound(Wire(1, 0x0C, CommEventLogCodec.EncodeResponse(0x0000, 0x0003, 0x0003, events)));

        (ushort status, ushort eventCount, ushort messageCount, byte[] decoded) = await client.GetCommEventLogAsync(1);
        Assert.AreEqual((ushort)0x0000, status);
        Assert.AreEqual((ushort)0x0003, eventCount);
        Assert.AreEqual((ushort)0x0003, messageCount);
        CollectionAssert.AreEqual(events, decoded);
    }

    [TestMethod]
    public async Task ReportServerIdRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        byte[] id = [0x01, 0x02, 0x03];
        transport.EnqueueInbound(Wire(1, 0x11, ServerIdCodec.EncodeResponse(id, true, [])));

        (byte[] serverId, bool running) = await client.ReportServerIdAsync(1);
        CollectionAssert.AreEqual(id, serverId);
        Assert.IsTrue(running);
    }

    [TestMethod]
    public async Task FileReadRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        FileSubRequest[] subs = [new(0x0004, 0x0001, 2)];
        ushort[][] records = [[0x0DFE, 0x0020]];
        transport.EnqueueInbound(Wire(1, 0x14, FileRecordCodec.EncodeReadResponse(records)));

        ushort[][] actual = await client.ReadFileRecordAsync(1, subs);
        Assert.HasCount(1, actual);
        CollectionAssert.AreEqual(records[0], actual[0]);
        CollectionAssert.AreEqual(Wire(1, 0x14, FileRecordCodec.EncodeReadRequest(subs)), transport.TakeWritten());
    }

    [TestMethod]
    public async Task FileWriteEchoVerified()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        FileSubRequest[] subs = [new(0x0004, 0x0007, 2)];
        ushort[][] datas = [[0x0006, 0x0009]];
        transport.EnqueueInbound(Wire(1, 0x15, FileRecordCodec.EncodeWriteResponse(subs)));

        await client.WriteFileRecordAsync(1, subs, datas);
    }

    [TestMethod]
    public async Task MaskWriteEchoVerified()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        transport.EnqueueInbound(Wire(1, 0x16, MaskWriteCodec.Encode(0x0004, 0x00F2, 0x0025)));

        await client.MaskWriteRegisterAsync(1, 0x0004, 0x00F2, 0x0025);
    }

    [TestMethod]
    public async Task ReadWriteMultipleRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        ushort[] readValues = [1, 2, 3];
        ushort[] writeValues = [9, 8];
        transport.EnqueueInbound(Wire(1, 0x17, ReadWriteMultipleCodec.EncodeResponse(readValues)));

        CollectionAssert.AreEqual(
            readValues,
            await client.ReadWriteMultipleRegistersAsync(1, 0, (ushort)readValues.Length, 10, writeValues));
    }

    [TestMethod]
    public async Task FifoReadRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        ushort[] values = [0x01AE, 0x0124];
        transport.EnqueueInbound(Wire(1, 0x18, FifoQueueCodec.EncodeResponse(values)));

        CollectionAssert.AreEqual(values, await client.ReadFifoQueueAsync(1, 4));
    }
}
