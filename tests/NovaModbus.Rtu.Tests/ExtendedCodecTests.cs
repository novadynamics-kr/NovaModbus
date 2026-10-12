using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Enums;
using NovaModbus.Rtu.Errors;
using NovaModbus.Rtu.Tests.Testing;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class ExtendedCodecTests
{
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
    public void ExceptionStatusRoundTrip()
    {
        Assert.HasCount(0, ExceptionStatusCodec.EncodeRequest());
        Assert.AreEqual((byte)0x5A, ExceptionStatusCodec.DecodeResponse([0x5A]));
        Assert.ThrowsExactly<ArgumentException>(() => ExceptionStatusCodec.DecodeResponse([0x01, 0x02]));
    }

    [TestMethod]
    public void DiagnosticsRoundTrip()
    {
        byte[] payload = [0x12, 0x34];
        byte[] body = DiagnosticsCodec.Encode(0x0000, payload);
        DiagnosticsCodec.Decode(body, out ushort sub, out byte[] data);
        Assert.AreEqual((ushort)0x0000, sub);
        CollectionAssert.AreEqual(payload, data);
    }

    [TestMethod]
    public void DiagnosticsCounterDataBigEndian()
    {
        byte[] data = DiagnosticsCodec.CounterData(0x1234);
        byte[] expected = [0x12, 0x34];
        CollectionAssert.AreEqual(expected, data);

        byte[] body = DiagnosticsCodec.Encode(DiagnosticsCodec.Subfunctions.ReturnBusMessageCount, data);
        DiagnosticsCodec.Decode(body, out ushort sub, out byte[] decoded);
        Assert.AreEqual(DiagnosticsCodec.Subfunctions.ReturnBusMessageCount, sub);
        CollectionAssert.AreEqual(expected, decoded);
    }

    [TestMethod]
    public void CommEventCounterRoundTrip()
    {
        byte[] body = CommEventCounterCodec.EncodeResponse(0x0000, 0x0064);
        CommEventCounterCodec.DecodeResponse(body, out ushort status, out ushort count);
        Assert.AreEqual((ushort)0x0000, status);
        Assert.AreEqual((ushort)0x0064, count);
    }

    [TestMethod]
    public void CommEventLogRoundTrip()
    {
        byte[] events = [0x70, 0x00];
        byte[] body = CommEventLogCodec.EncodeResponse(0x0000, 0x0003, 0x0003, events);
        CommEventLogCodec.DecodeResponse(body, out ushort status, out ushort eventCount, out ushort messageCount, out byte[] decoded);
        Assert.AreEqual((ushort)0x0000, status);
        Assert.AreEqual((ushort)0x0003, eventCount);
        Assert.AreEqual((ushort)0x0003, messageCount);
        CollectionAssert.AreEqual(events, decoded);
    }

    [TestMethod]
    public void ServerIdRoundTrip()
    {
        byte[] id = [0x4E, 0x6F, 0x76, 0x61];
        byte[] body = ServerIdCodec.EncodeResponse(id, true, []);
        ServerIdCodec.DecodeResponse(body, out byte[] decodedId, out bool running);
        CollectionAssert.AreEqual(id, decodedId);
        Assert.IsTrue(running);
    }

    [TestMethod]
    public void ServerIdFullSplitWithAdditional()
    {
        byte[] id = [0x01, 0x02];
        byte[] additional = [0xAA, 0xBB];
        byte[] body = ServerIdCodec.EncodeResponse(id, false, additional);
        ServerIdCodec.DecodeResponse(body, id.Length, out byte[] decodedId, out bool running, out byte[] decodedAdditional);
        CollectionAssert.AreEqual(id, decodedId);
        Assert.IsFalse(running);
        CollectionAssert.AreEqual(additional, decodedAdditional);
    }

    [TestMethod]
    public void FileReadRoundTrip()
    {
        FileSubRequest[] subs = [new(0x0004, 0x0001, 2), new(0x0004, 0x0003, 1)];
        byte[] request = FileRecordCodec.EncodeReadRequest(subs);
        CollectionAssert.AreEqual(subs, FileRecordCodec.DecodeReadRequest(request));

        ushort[][] records = [[0x0DFE, 0x0020], [0x00FF]];
        byte[] response = FileRecordCodec.EncodeReadResponse(records);
        ushort[][] decoded = FileRecordCodec.DecodeReadResponse(response);
        Assert.HasCount(2, decoded);
        CollectionAssert.AreEqual(records[0], decoded[0]);
        CollectionAssert.AreEqual(records[1], decoded[1]);
    }

    [TestMethod]
    public void FileReadBadReferenceTypeRejected()
    {
        FileSubRequest[] subs = [new(1, 0, 1)];
        byte[] request = FileRecordCodec.EncodeReadRequest(subs);
        request[1] = 0x05;
        Assert.ThrowsExactly<ArgumentException>(() => FileRecordCodec.DecodeReadRequest(request));
    }

    [TestMethod]
    public void FileWriteRoundTrip()
    {
        FileSubRequest[] subs = [new(0x0004, 0x0007, 2)];
        ushort[][] datas = [[0x0006, 0x0009]];
        byte[] request = FileRecordCodec.EncodeWriteRequest(subs, datas);
        FileRecordCodec.DecodeWriteRequest(request, out FileSubRequest[] decodedSubs, out ushort[][] decodedDatas);
        CollectionAssert.AreEqual(subs, decodedSubs);
        CollectionAssert.AreEqual(datas[0], decodedDatas[0]);

        byte[] response = FileRecordCodec.EncodeWriteResponse(subs);
        CollectionAssert.AreEqual(subs, FileRecordCodec.DecodeWriteResponse(response));
    }

    [TestMethod]
    public void MaskWriteRoundTripAndAlgorithm()
    {
        byte[] body = MaskWriteCodec.Encode(0x0004, 0x00F2, 0x0025);
        MaskWriteCodec.Decode(body, out ushort address, out ushort andMask, out ushort orMask);
        Assert.AreEqual((ushort)0x0004, address);
        Assert.AreEqual((ushort)0x00F2, andMask);
        Assert.AreEqual((ushort)0x0025, orMask);
        // Spec example: current 0x0012 -> (0x0012 & 0x00F2) | (0x0025 & ~0x00F2) = 0x0017.
        Assert.AreEqual((ushort)0x0017, MaskWriteCodec.Apply(0x0012, 0x00F2, 0x0025));
    }

    [TestMethod]
    public void ReadWriteMultipleRoundTrip()
    {
        ushort[] writeValues = [0x00FF, 0x00FF];
        byte[] request = ReadWriteMultipleCodec.EncodeRequest(0x0003, 6, 0x000E, writeValues);
        ReadWriteMultipleCodec.DecodeRequest(request, out ushort rs, out ushort rc, out ushort ws, out ushort[] decoded);
        Assert.AreEqual((ushort)0x0003, rs);
        Assert.AreEqual((ushort)6, rc);
        Assert.AreEqual((ushort)0x000E, ws);
        CollectionAssert.AreEqual(writeValues, decoded);

        ushort[] readValues = [1, 2, 3, 4, 5, 6];
        CollectionAssert.AreEqual(readValues, ReadWriteMultipleCodec.DecodeResponse(ReadWriteMultipleCodec.EncodeResponse(readValues)));
    }

    [TestMethod]
    public void ReadWriteMultipleWriteOver121Rejected()
    {
        // 11 + 2N ADU cap: N=121 is the last legal write count.
        ushort[] max = new ushort[121];
        Assert.HasCount(9 + (121 * 2), ReadWriteMultipleCodec.EncodeRequest(0, 1, 0, max));

        ushort[] over = new ushort[122];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => ReadWriteMultipleCodec.EncodeRequest(0, 1, 0, over));
    }

    [TestMethod]
    public void SlaveDropsReadWriteMultipleOver121Frame()
    {
        ModbusRtuSlave slave = new(new FakeSerialTransport());
        slave.Engine.AddSlave(1);

        // 122 write registers need a 257-byte ADU: dropped at the frame cap.
        byte[] body = new byte[9 + (122 * 2)];
        body[8] = (byte)(122 * 2);
        Assert.IsNull(slave.HandleRequest(new ModbusFrame(1, 0x17, body).Encode()));
        Assert.AreEqual(1L, slave.Engine.BusCrcErrorCount);
    }

    [TestMethod]
    public void SlaveRejectsReadWriteMultipleByteCountMismatch()
    {
        ModbusRtuSlave slave = new(new FakeSerialTransport());
        slave.Engine.AddSlave(1);

        byte[] body = ReadWriteMultipleCodec.EncodeRequest(0, 1, 0, [1, 2]);
        body[8] = 0xFF;
        byte[]? response = slave.HandleRequest(new ModbusFrame(1, 0x17, body).Encode());
        Assert.AreEqual(ModbusExceptionCode.IllegalDataValue, DecodeException(response, 0x17));
    }

    [TestMethod]
    public void FileReadResponseOverPduCapRejected()
    {
        // Two full-length records exceed the 253-byte PDU cap.
        ushort[][] records = [new ushort[125], new ushort[125]];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => FileRecordCodec.EncodeReadResponse(records));
    }

    [TestMethod]
    public void SlaveReportsFailureOnOversizeFileRead()
    {
        ModbusRtuSlave slave = new(new FakeSerialTransport());
        slave.Engine.AddSlave(1);
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        store.EnsureFile(1, 250);

        FileSubRequest[] subs = [new(1, 0, 125), new(1, 125, 125)];
        byte[]? response = slave.HandleRequest(
            new ModbusFrame(1, 0x14, FileRecordCodec.EncodeReadRequest(subs)).Encode());
        Assert.AreEqual(ModbusExceptionCode.SlaveDeviceFailure, DecodeException(response, 0x14));
    }

    [TestMethod]
    public void FifoQueueRoundTrip()
    {
        ushort[] values = [0x01AE, 0x0124, 0x0128];
        byte[] body = FifoQueueCodec.EncodeResponse(values);
        CollectionAssert.AreEqual(values, FifoQueueCodec.DecodeResponse(body));
        Assert.AreEqual((ushort)0x0004, FifoQueueCodec.DecodeRequest(FifoQueueCodec.EncodeRequest(0x0004)));
    }

    [TestMethod]
    public void FifoOverLimitRejected()
    {
        ushort[] tooMany = new ushort[32];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FifoQueueCodec.EncodeResponse(tooMany));
    }

    [TestMethod]
    public void NegativeAcknowledgeRoundTrip()
    {
        byte[] body = ExceptionCodec.Encode(ModbusExceptionCode.NegativeAcknowledge);
        Assert.AreEqual(ModbusExceptionCode.NegativeAcknowledge, ExceptionCodec.Decode(body));
    }
}
