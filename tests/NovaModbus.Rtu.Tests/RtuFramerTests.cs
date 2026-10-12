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

        // 5ms chunk gap is inside both overrides.
        byte[]? frame = await RtuFramer.ReadFrameAsync(
            transport, 9600, Timeout, Silence, interCharIntervalOverride: Silence);
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
            () => RtuFramer.ReadFrameAsync(transport, 9600, Timeout, Silence, null, cts.Token));
    }

    [TestMethod]
    public void DerivedIntervalMatchesCharTiming()
    {
        // 11 bits * 3.5 chars at 9600bps = 4.01ms.
        TimeSpan interval = RtuFramer.SilentInterval(9600);
        Assert.IsTrue(interval.TotalMilliseconds is >= 4.0 and < 5.0);
        // Above 19200bps the spec mandates fixed timers (1.75ms / 750µs).
        Assert.AreEqual(TimeSpan.FromMilliseconds(1.75), RtuFramer.SilentInterval(115200));
        Assert.AreEqual(TimeSpan.FromMilliseconds(0.75), RtuFramer.InterCharInterval(115200));
        TimeSpan interChar = RtuFramer.InterCharInterval(9600);
        Assert.IsTrue(interChar.TotalMilliseconds is >= 1.7 and < 1.8);
    }

    [TestMethod]
    public async Task GapWithinInterCharJoinsFrame()
    {
        await using FakeSerialTransport transport = new();
        ModbusFrame request = new(0x01, 0x03, [0x00, 0x00, 0x00, 0x0A]);
        byte[] wire = request.Encode();
        TimeSpan silence = TimeSpan.FromMilliseconds(100);
        TimeSpan interChar = TimeSpan.FromMilliseconds(20);

        Task<byte[]?> read = RtuFramer.ReadFrameAsync(
            transport, 9600, Timeout, silence, interChar);
        transport.EnqueueInbound(wire.AsSpan(0, 3));
        await Task.Delay(5);
        transport.EnqueueInbound(wire.AsSpan(3));

        byte[]? frame = await read;
        Assert.IsNotNull(frame);
        CollectionAssert.AreEqual(wire, frame);
    }

    [TestMethod]
    public async Task GapOverInterCharDiscardsPartialFrame()
    {
        await using FakeSerialTransport transport = new();
        ModbusFrame request = new(0x01, 0x03, [0x00, 0x00, 0x00, 0x0A]);
        byte[] wire = request.Encode();
        TimeSpan silence = TimeSpan.FromMilliseconds(100);
        TimeSpan interChar = TimeSpan.FromMilliseconds(10);

        Task<byte[]?> read = RtuFramer.ReadFrameAsync(
            transport, 9600, Timeout, silence, interChar);
        transport.EnqueueInbound(wire.AsSpan(0, 3));
        // 30ms gap exceeds t1.5 (10ms) but not t3.5 (100ms): first chunk dropped,
        // reception restarts at the late bytes.
        await Task.Delay(30);
        transport.EnqueueInbound(wire.AsSpan(3));

        byte[]? frame = await read;
        Assert.IsNotNull(frame);
        CollectionAssert.AreEqual(wire.AsSpan(3).ToArray(), frame);
    }

    [TestMethod]
    public async Task OversizeFrameDiscarded()
    {
        await using FakeSerialTransport transport = new();
        byte[] noise = new byte[RtuFramer.MaxFrameSize + 44];
        Random.Shared.NextBytes(noise);
        transport.EnqueueInbound(noise);

        byte[]? frame = await RtuFramer.ReadFrameAsync(
            transport, 9600, Timeout, silentIntervalOverride: Silence);
        Assert.IsNull(frame);
    }
}
