using NovaModbus.Rtu.Transport;
using NovaModbus.Rtu.Tests.Testing;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class RtuFramerTests
{
    private static readonly TimeSpan Silence = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    [TestMethod]
    public async Task SingleFrameReceivedIntact()
    {
        await using FakeSerialTransport transport = new();
        ModbusFrame request = new(0x01, 0x03, [0x00, 0x00, 0x00, 0x0A]);
        byte[] wire = request.Encode();
        transport.EnqueueInbound(wire);

        byte[]? frame = await RtuFramer.ReadFrameAsync(transport, 9600, Timeout, silentIntervalOverride: Silence);
        Assert.IsNotNull(frame);
        CollectionAssert.AreEqual(wire, frame);
    }

    [TestMethod]
    public async Task ChunkedArrivalJoinedWithinSilence()
    {
        await using FakeSerialTransport transport = new();
        ModbusFrame request = new(0x01, 0x03, [0x00, 0x00, 0x00, 0x0A]);
        byte[] wire = request.Encode();
        transport.EnqueueInbound(wire.AsSpan(0, 3));
        await Task.Delay(5);
        transport.EnqueueInbound(wire.AsSpan(3));

        byte[]? frame = await RtuFramer.ReadFrameAsync(transport, 9600, Timeout, silentIntervalOverride: Silence);
        Assert.IsNotNull(frame);
        CollectionAssert.AreEqual(wire, frame);
    }

    [TestMethod]
    public async Task SilentGapSplitsTwoFrames()
    {
        await using FakeSerialTransport transport = new();
        ModbusFrame first = new(0x01, 0x03, [0x00, 0x00, 0x00, 0x0A]);
        ModbusFrame second = new(0x02, 0x03, [0x00, 0x00, 0x00, 0x02]);
        byte[] wire1 = first.Encode();
        byte[] wire2 = second.Encode();

        Task<byte[]?> read1 = RtuFramer.ReadFrameAsync(transport, 9600, Timeout, silentIntervalOverride: Silence);
        transport.EnqueueInbound(wire1);
        byte[]? frame1 = await read1;
        Assert.IsNotNull(frame1);
        CollectionAssert.AreEqual(wire1, frame1);

        await Task.Delay(100);
        Task<byte[]?> read2 = RtuFramer.ReadFrameAsync(transport, 9600, Timeout, silentIntervalOverride: Silence);
        transport.EnqueueInbound(wire2);
        byte[]? frame2 = await read2;
        Assert.IsNotNull(frame2);
        CollectionAssert.AreEqual(wire2, frame2);
    }

    [TestMethod]
    public async Task NoDataReturnsNull()
    {
        await using FakeSerialTransport transport = new();
        byte[]? frame = await RtuFramer.ReadFrameAsync(
            transport, 9600, TimeSpan.FromMilliseconds(50), silentIntervalOverride: Silence);
        Assert.IsNull(frame);
    }

    [TestMethod]
    public async Task UserCancellationThrows()
    {
        await using FakeSerialTransport transport = new();
        using CancellationTokenSource cts = new();
        cts.CancelAfter(20);
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => RtuFramer.ReadFrameAsync(transport, 9600, Timeout, Silence, cts.Token));
    }

    [TestMethod]
    public void DerivedIntervalMatchesCharTiming()
    {
        // 11 bits * 3.5 chars at 9600bps = 4.01ms.
        TimeSpan interval = RtuFramer.SilentInterval(9600);
        Assert.IsTrue(interval.TotalMilliseconds is >= 4.0 and < 5.0);
        Assert.AreEqual(TimeSpan.FromMilliseconds(2.0), RtuFramer.SilentInterval(115200));
    }
}
