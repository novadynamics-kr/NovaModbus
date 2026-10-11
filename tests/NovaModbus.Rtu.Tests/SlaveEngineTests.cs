namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class SlaveEngineTests
{
    [TestMethod]
    public void AddAndRouteMultipleIdsWithIndependentData()
    {
        ModbusSlaveEngine engine = new();
        Assert.IsTrue(engine.AddSlave(1));
        Assert.IsTrue(engine.AddSlave(2));
        Assert.IsTrue(engine.TryGetStore(1, out ModbusDataStore? s1));
        Assert.IsTrue(engine.TryGetStore(2, out ModbusDataStore? s2));
        Assert.IsNotNull(s1);
        Assert.IsNotNull(s2);

        s1.WriteHolding(0, 111);
        s2.WriteHolding(0, 222);
        Assert.AreEqual((ushort)111, s1.ReadHolding(0, 1)[0]);
        Assert.AreEqual((ushort)222, s2.ReadHolding(0, 1)[0]);
        CollectionAssert.AreEquivalent(new byte[] { 1, 2 }, engine.SlaveIds);
    }

    [TestMethod]
    public void ReservedIdsRejected()
    {
        ModbusSlaveEngine engine = new();
        Assert.IsFalse(engine.AddSlave(0));
        Assert.IsFalse(engine.AddSlave(248));
        Assert.HasCount(0, engine.SlaveIds);
    }

    [TestMethod]
    public void RemoveSlaveClearsRoutingAndCounter()
    {
        ModbusSlaveEngine engine = new();
        engine.AddSlave(5);
        engine.IncrementCount(5);
        Assert.IsTrue(engine.RemoveSlave(5));
        Assert.IsFalse(engine.TryGetStore(5, out _));
        Assert.AreEqual(0, engine.GetCount(5));
    }

    [TestMethod]
    public void PerIdCountersAreIndependent()
    {
        ModbusSlaveEngine engine = new();
        engine.AddSlave(1);
        engine.AddSlave(2);
        engine.IncrementCount(1);
        engine.IncrementCount(1);
        engine.IncrementCount(2);
        Assert.AreEqual(2, engine.GetCount(1));
        Assert.AreEqual(1, engine.GetCount(2));
    }
}
