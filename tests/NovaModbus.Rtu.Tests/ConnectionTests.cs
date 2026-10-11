using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class ConnectionTests
{
    [TestMethod]
    public async Task VirtualConnectReadWriteDisconnect()
    {
        VirtualSerialBus bus = new();
        var (held, published) = VirtualTransport.CreatePair();
        await using (held)
        await using (published)
        {
            ModbusRtuSlave slave = new(held);
            slave.Engine.AddSlave(1);
            SlaveServer server = new(slave);
            using CancellationTokenSource cts = new();
            Task run = server.RunAsync(cts.Token);
            bus.Publish("t1", published);

            await using ModbusConnection connection = new();
            Assert.IsFalse(connection.IsConnected);
            await connection.ConnectVirtualAsync("t1", bus, 9600);
            Assert.IsTrue(connection.IsConnected);
            Assert.IsNotNull(connection.Client);

            ushort[] seed = [0x1234];
            await connection.Client.WriteMultipleRegistersAsync(1, 0, seed);
            CollectionAssert.AreEqual(seed, await connection.Client.ReadHoldingRegistersAsync(1, 0, 1));

            await connection.DisconnectAsync();
            Assert.IsFalse(connection.IsConnected);

            cts.Cancel();
            await run;
            Assert.IsNull(server.LastError);
        }
    }

    [TestMethod]
    public async Task ConnectMissingVirtualThrows()
    {
        VirtualSerialBus bus = new();
        await using ModbusConnection connection = new();
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => connection.ConnectVirtualAsync("missing", bus, 9600));
        Assert.IsFalse(connection.IsConnected);
    }

    [TestMethod]
    public async Task DisconnectWhenIdleIsSafe()
    {
        await using ModbusConnection connection = new();
        await connection.DisconnectAsync();
        Assert.IsFalse(connection.IsConnected);
    }

    [TestMethod]
    public async Task ConnectMissingSerialPortThrows()
    {
        await using ModbusConnection connection = new();
        try
        {
            await connection.ConnectSerialAsync(new SerialSettings("COM99"));
            Assert.Fail("Expected IOException for a missing port.");
        }
        catch (IOException)
        {
            // Missing ports surface as IOException subtypes (e.g. FileNotFoundException).
        }

        Assert.IsFalse(connection.IsConnected);
    }
}
