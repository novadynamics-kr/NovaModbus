using System.Diagnostics;
using NovaModbus.Rtu.Codecs;
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

    [TestMethod]
    public async Task BroadcastWriteAppliesToAllSlavesWithNoResponse()
    {
        var (clientTransport, slaveTransport) = FakeSerialTransport.CreatePair();
        await using ModbusRtuClient client = new(clientTransport);
        await using ModbusRtuSlave slave = new(slaveTransport);
        slave.Engine.AddSlave(1);
        slave.Engine.AddSlave(2);
        using CancellationTokenSource cts = new();
        Task serve = ServeLoopAsync(slave, cts.Token);

        try
        {
            // Fire-and-forget: returns without waiting for a (nonexistent) response.
            await client.BroadcastWriteSingleRegisterAsync(10, 0x55AA);

            // The serve loop applies the write to every store; spin until visible.
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(5))
            {
                slave.Engine.TryGetStore(1, out ModbusDataStore? first);
                slave.Engine.TryGetStore(2, out ModbusDataStore? second);
                if (first?.ReadHolding(10, 1)[0] == 0x55AA
                    && second?.ReadHolding(10, 1)[0] == 0x55AA)
                {
                    return;
                }

                await Task.Delay(5);
            }

            Assert.Fail("Broadcast write was not applied to all slave stores.");
        }
        finally
        {
            cts.Cancel();
            await serve;
        }
    }

    [TestMethod]
    public async Task BroadcastReadIgnored()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuSlave slave = new(transport);
        slave.Engine.AddSlave(1);

        byte[] wire = new ModbusFrame(
            ModbusLimits.BroadcastAddress, 0x03, ReadRegistersCodec.EncodeRequest(0, 1)).Encode();
        Assert.IsNull(slave.HandleRequest(wire));
        Assert.AreEqual(0L, slave.Engine.GetCount(1));
    }

    [TestMethod]
    public async Task BroadcastDiagnosticsIgnored()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuSlave slave = new(transport);
        slave.Engine.AddSlave(1);

        // Seed the message counter with one unicast write.
        byte[] seed = new ModbusFrame(
            1, 0x06, WriteSingleRegisterCodec.Encode(0, 1)).Encode();
        Assert.IsNotNull(slave.HandleRequest(seed));
        slave.Engine.TryGetStore(1, out ModbusDataStore? store);
        Assert.IsNotNull(store);
        Assert.AreEqual(1, store.SlaveMessageCount);

        // Broadcast ClearCounters must neither respond nor clear (FC08: no broadcast).
        byte[] wire = new ModbusFrame(
            ModbusLimits.BroadcastAddress, 0x08, DiagnosticsCodec.Encode(0x000A, [0x00, 0x00])).Encode();
        Assert.IsNull(slave.HandleRequest(wire));
        Assert.AreEqual(1, store.SlaveMessageCount);
    }

    [TestMethod]
    public async Task BroadcastEnforcesTransmitQuiet()
    {
        await using FakeSerialTransport transport = new();
        await using ModbusRtuClient client = new(transport);

        await client.BroadcastWriteSingleCoilAsync(0, true);
        Stopwatch sw = Stopwatch.StartNew();
        await client.BroadcastWriteSingleCoilAsync(0, false);
        sw.Stop();

        // t3.5 at 9600bps is ~4.01ms; allow a 1ms timer margin.
        Assert.IsTrue(sw.Elapsed.TotalMilliseconds >= 3.0);
    }
}
