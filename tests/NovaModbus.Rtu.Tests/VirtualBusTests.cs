using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class VirtualBusTests
{
    [TestMethod]
    public async Task PairRoundTrip()
    {
        var (left, right) = VirtualTransport.CreatePair();
        await using (left)
        await using (right)
        {
            byte[] payload = [0x01, 0x03, 0x00, 0x00];
            await left.WriteAsync(payload);
            byte[] received = new byte[16];
            int read = await right.ReadAsync(received);
            Assert.AreEqual(payload.Length, read);
            CollectionAssert.AreEqual(payload, received[..read]);
        }
    }

    [TestMethod]
    public async Task NamedHandoffIsExclusive()
    {
        VirtualSerialBus bus = new();
        var (held, published) = VirtualTransport.CreatePair();
        await using (held)
        await using (published)
        {
            bus.Publish("Slave1", published);
            string[] expected = ["Slave1"];
            CollectionAssert.AreEqual(expected, bus.ListEndpoints());

            ISerialTransport taken = bus.Connect("Slave1");
            Assert.HasCount(0, bus.ListEndpoints());
            Assert.ThrowsExactly<InvalidOperationException>(() => bus.Connect("Slave1"));

            byte[] payload = [0xAA];
            await taken.WriteAsync(payload);
            byte[] received = new byte[4];
            int read = await held.ReadAsync(received);
            Assert.AreEqual(1, read);
            Assert.AreEqual(0xAA, received[0]);
        }
    }

    [TestMethod]
    public void ConnectMissingThrowsAndUnpublishFalse()
    {
        VirtualSerialBus bus = new();
        Assert.ThrowsExactly<InvalidOperationException>(() => bus.Connect("Nope"));
        Assert.IsFalse(bus.Unpublish("Nope"));
    }

    [TestMethod]
    public async Task FullFrameThroughVirtualPair()
    {
        var (left, right) = VirtualTransport.CreatePair();
        await using (left)
        await using (right)
        {
            ModbusFrame request = new(0x01, 0x03, [0x00, 0x00, 0x00, 0x0A]);
            byte[] wire = request.Encode();
            await left.WriteAsync(wire);

            byte[]? frame = await RtuFramer.ReadFrameAsync(
                right, 9600, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(10));
            Assert.IsNotNull(frame);
            CollectionAssert.AreEqual(wire, frame);
        }
    }
}
