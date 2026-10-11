namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class DataStoreTests
{
    [TestMethod]
    public void HoldingReadWriteRoundTrip()
    {
        ModbusDataStore store = new(coilCount: 10, discreteCount: 10, holdingCount: 10, inputCount: 10);
        ushort[] seed = [0x1234, 0xABCD];
        store.WriteHolding(2, seed);
        CollectionAssert.AreEqual(seed, store.ReadHolding(2, 2));
    }

    [TestMethod]
    public void HoldingOutOfRangeThrows()
    {
        ModbusDataStore store = new(coilCount: 10, discreteCount: 10, holdingCount: 10, inputCount: 10);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => store.ReadHolding(9, 2));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => store.WriteHolding(10, 1));
    }

    [TestMethod]
    public void CoilsReadWriteRoundTrip()
    {
        ModbusDataStore store = new(coilCount: 16, discreteCount: 16, holdingCount: 16, inputCount: 16);
        bool[] seed = [true, false, true];
        store.WriteCoils(5, seed);
        CollectionAssert.AreEqual(seed, store.ReadCoils(5, 3));
        store.WriteCoil(0, true);
        Assert.IsTrue(store.ReadCoils(0, 1)[0]);
    }

    [TestMethod]
    public void CoilsOutOfRangeThrows()
    {
        ModbusDataStore store = new(coilCount: 8, discreteCount: 8, holdingCount: 8, inputCount: 8);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => store.ReadCoils(7, 2));
    }

    [TestMethod]
    public void DiscreteAndInputSeedAndRead()
    {
        ModbusDataStore store = new(coilCount: 8, discreteCount: 8, holdingCount: 8, inputCount: 8);
        bool[] discreteSeed = [true, true, false];
        store.WriteDiscrete(1, discreteSeed);
        CollectionAssert.AreEqual(discreteSeed, store.ReadDiscrete(1, 3));
        ushort[] inputSeed = [100, 200];
        store.WriteInput(0, inputSeed);
        CollectionAssert.AreEqual(inputSeed, store.ReadInput(0, 2));
    }

    [TestMethod]
    public void DiscreteOutOfRangeThrows()
    {
        ModbusDataStore store = new(coilCount: 8, discreteCount: 8, holdingCount: 8, inputCount: 8);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => store.ReadDiscrete(8, 1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => store.ReadInput(7, 2));
    }
}
