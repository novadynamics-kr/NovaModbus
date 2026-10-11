using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Errors;
using NovaModbus.Rtu.Tests.Testing;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class TransactionTests
{
    private static readonly TimeSpan Silence = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    private static byte[] ResponseWire(byte slaveId, byte function, byte[] body) =>
        new ModbusFrame(slaveId, function, body).Encode();

    [TestMethod]
    public async Task ExchangeReturnsResponseData()
    {
        await using FakeSerialTransport transport = new();
        ushort[] values = [0x022B, 0x0000, 0x0064];
        transport.EnqueueInbound(ResponseWire(1, 0x03, ReadRegistersCodec.EncodeResponse(values)));

        byte[] data = await ModbusTransaction.ExchangeAsync(
            transport, 9600, 1, 0x03, ReadRegistersCodec.EncodeRequest(0x006B, 3),
            Timeout, silentIntervalOverride: Silence);
        CollectionAssert.AreEqual(values, ReadRegistersCodec.DecodeResponse(data));
    }

    [TestMethod]
    public async Task GarbageFrameIgnoredThenValidAccepted()
    {
        await using FakeSerialTransport transport = new();
        Task<byte[]> exchange = ModbusTransaction.ExchangeAsync(
            transport, 9600, 1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1),
            Timeout, silentIntervalOverride: Silence);
        transport.EnqueueInbound([0xFF, 0x00, 0x11]);
        await Task.Delay(50);
        ushort[] values = [7];
        transport.EnqueueInbound(ResponseWire(1, 0x03, ReadRegistersCodec.EncodeResponse(values)));

        byte[] data = await exchange;
        CollectionAssert.AreEqual(values, ReadRegistersCodec.DecodeResponse(data));
    }

    [TestMethod]
    public async Task ForeignSlaveAndFunctionIgnoredThenValidAccepted()
    {
        await using FakeSerialTransport transport = new();
        Task<byte[]> exchange = ModbusTransaction.ExchangeAsync(
            transport, 9600, 1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1),
            Timeout, silentIntervalOverride: Silence);
        transport.EnqueueInbound(ResponseWire(2, 0x03, ReadRegistersCodec.EncodeResponse([1])));
        await Task.Delay(100);
        transport.EnqueueInbound(ResponseWire(1, 0x04, ReadRegistersCodec.EncodeResponse([2])));
        await Task.Delay(100);
        ushort[] values = [9];
        transport.EnqueueInbound(ResponseWire(1, 0x03, ReadRegistersCodec.EncodeResponse(values)));

        byte[] data = await exchange;
        CollectionAssert.AreEqual(values, ReadRegistersCodec.DecodeResponse(data));
    }

    [TestMethod]
    public async Task ExceptionResponseThrowsSlaveException()
    {
        await using FakeSerialTransport transport = new();
        transport.EnqueueInbound(ResponseWire(1, 0x83, [0x02]));

        ModbusSlaveException ex = await Assert.ThrowsExactlyAsync<ModbusSlaveException>(
            () => ModbusTransaction.ExchangeAsync(
                transport, 9600, 1, 0x03, ReadRegistersCodec.EncodeRequest(0, 1),
                Timeout, silentIntervalOverride: Silence));
        Assert.AreEqual((byte)0x02, ex.ExceptionCode);
    }

    [TestMethod]
    public async Task NoResponseThrowsTimeoutAfterRetries()
    {
        await using FakeSerialTransport transport = new();
        byte[] requestBody = ReadRegistersCodec.EncodeRequest(0, 1);
        int expectedWrites = 2 * new ModbusFrame(1, 0x03, requestBody).Encode().Length;

        await Assert.ThrowsExactlyAsync<TimeoutException>(
            () => ModbusTransaction.ExchangeAsync(
                transport, 9600, 1, 0x03, requestBody,
                TimeSpan.FromMilliseconds(80), 1, Silence));
        Assert.HasCount(expectedWrites, transport.TakeWritten());
    }

    [TestMethod]
    public async Task InvalidSlaveIdRejected()
    {
        await using FakeSerialTransport transport = new();
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => ModbusTransaction.ExchangeAsync(
                transport, 9600, 0, 0x03, [0, 0, 0, 1], Timeout));
    }
}
