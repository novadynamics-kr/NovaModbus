using NovaModbus.Rtu.Errors;
using NovaModbus.Rtu.Tests.Testing;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class LoopbackTests
{
    [TestMethod]
    public async Task ClientSlaveReadWriteLoopback()
    {
        var (clientTransport, slaveTransport) = FakeSerialTransport.CreatePair();
        await using ModbusRtuClient client = new(clientTransport);
        await using ModbusRtuSlave slave = new(slaveTransport);
        slave.Engine.AddSlave(1);
        using CancellationTokenSource cts = new();
        Task serve = ServeLoopAsync(slave, cts.Token);

        ushort[] initial = await client.ReadHoldingRegistersAsync(1, 0, 2);
        Assert.HasCount(2, initial);

        ushort[] seed = [0x1234, 0xABCD];
        await client.WriteMultipleRegistersAsync(1, 0, seed);
        CollectionAssert.AreEqual(seed, await client.ReadHoldingRegistersAsync(1, 0, 2));

        await client.WriteSingleCoilAsync(1, 3, true);
        bool[] coils = await client.ReadCoilsAsync(1, 3, 1);
        Assert.HasCount(1, coils);
        Assert.IsTrue(coils[0]);

        cts.Cancel();
        await serve;
    }

    [TestMethod]
    public async Task SlaveExceptionSurfacesAsClientException()
    {
        var (clientTransport, slaveTransport) = FakeSerialTransport.CreatePair();
        await using ModbusRtuClient client = new(clientTransport);
        await using ModbusRtuSlave slave = new(slaveTransport);
        slave.Engine.AddSlave(1);
        using CancellationTokenSource cts = new();
        Task serve = ServeLoopAsync(slave, cts.Token);

        try
        {
            ModbusSlaveException ex = await Assert.ThrowsExactlyAsync<ModbusSlaveException>(
                () => client.ReadHoldingRegistersAsync(1, 0xFFFF, 2));
            Assert.AreEqual((byte)0x02, ex.ExceptionCode);
        }
        finally
        {
            cts.Cancel();
            await serve;
        }
    }

    private static async Task ServeLoopAsync(ModbusRtuSlave slave, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await slave.ServeOnceAsync(TimeSpan.FromMilliseconds(5), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
