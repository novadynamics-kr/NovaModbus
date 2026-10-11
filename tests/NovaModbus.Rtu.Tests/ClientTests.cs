using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Tests.Testing;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class ClientTests
{
    private static byte[] Wire(byte slaveId, byte function, byte[] body) =>
        new ModbusFrame(slaveId, function, body).Encode();

    [TestMethod]
    public async Task ReadCoilsRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        bool[] values = [true, false, true, true, false, false, false, true, true, false];
        transport.EnqueueInbound(Wire(1, 0x01, ReadBitsCodec.EncodeResponse(values)));

        bool[] actual = await client.ReadCoilsAsync(1, 0x0013, (ushort)values.Length);
        CollectionAssert.AreEqual(values, actual);
        CollectionAssert.AreEqual(Wire(1, 0x01, ReadBitsCodec.EncodeRequest(0x0013, (ushort)values.Length)), transport.TakeWritten());
    }

    [TestMethod]
    public async Task ReadDiscreteInputsRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        bool[] values = [false, true, true];
        transport.EnqueueInbound(Wire(1, 0x02, ReadBitsCodec.EncodeResponse(values)));

        bool[] actual = await client.ReadDiscreteInputsAsync(1, 5, (ushort)values.Length);
        CollectionAssert.AreEqual(values, actual);
    }

    [TestMethod]
    public async Task ReadHoldingRegistersRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        ushort[] values = [0x022B, 0x0000, 0x0064];
        transport.EnqueueInbound(Wire(1, 0x03, ReadRegistersCodec.EncodeResponse(values)));

        ushort[] actual = await client.ReadHoldingRegistersAsync(1, 0x006B, (ushort)values.Length);
        CollectionAssert.AreEqual(values, actual);
        CollectionAssert.AreEqual(Wire(1, 0x03, ReadRegistersCodec.EncodeRequest(0x006B, 3)), transport.TakeWritten());
    }

    [TestMethod]
    public async Task ReadInputRegistersRoundTrip()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        ushort[] values = [100, 200];
        transport.EnqueueInbound(Wire(1, 0x04, ReadRegistersCodec.EncodeResponse(values)));

        ushort[] actual = await client.ReadInputRegistersAsync(1, 0, (ushort)values.Length);
        CollectionAssert.AreEqual(values, actual);
    }

    [TestMethod]
    public async Task WriteSingleCoilEchoVerified()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        transport.EnqueueInbound(Wire(1, 0x05, WriteSingleCoilCodec.Encode(0x00AC, true)));

        await client.WriteSingleCoilAsync(1, 0x00AC, true);
        CollectionAssert.AreEqual(Wire(1, 0x05, WriteSingleCoilCodec.Encode(0x00AC, true)), transport.TakeWritten());
    }

    [TestMethod]
    public async Task WriteSingleRegisterEchoVerified()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        transport.EnqueueInbound(Wire(1, 0x06, WriteSingleRegisterCodec.Encode(0x0001, 0x0003)));

        await client.WriteSingleRegisterAsync(1, 0x0001, 0x0003);
    }

    [TestMethod]
    public async Task WriteMultipleCoilsEchoVerified()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        bool[] values = [true, false, true, false, true];
        transport.EnqueueInbound(Wire(1, 0x0F, WriteMultipleCoilsCodec.EncodeResponse(0x0013, (ushort)values.Length)));

        await client.WriteMultipleCoilsAsync(1, 0x0013, values);
    }

    [TestMethod]
    public async Task WriteMultipleRegistersEchoVerified()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        ushort[] values = [0x000A, 0x0102];
        transport.EnqueueInbound(Wire(1, 0x10, WriteMultipleRegistersCodec.EncodeResponse(0x0001, (ushort)values.Length)));

        await client.WriteMultipleRegistersAsync(1, 0x0001, values);
    }

    [TestMethod]
    public async Task EchoMismatchThrows()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);
        transport.EnqueueInbound(Wire(1, 0x05, WriteSingleCoilCodec.Encode(0x00AC, false)));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => client.WriteSingleCoilAsync(1, 0x00AC, true));
    }

    [TestMethod]
    public async Task NoResponseThrowsTimeout()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport)
        {
            Timeout = TimeSpan.FromMilliseconds(60),
            Retries = 0,
        };

        await Assert.ThrowsExactlyAsync<TimeoutException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 1));
    }
}
