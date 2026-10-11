using NovaModbus.Rtu.Codecs;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class Float32CodecTests
{
    [TestMethod]
    public void KnownVectorOneAbcd()
    {
        // 1.0f = 0x3F800000 -> ABCD [0x3F80, 0x0000].
        ushort[] regs = Float32Codec.Encode(1.0f, FloatWordOrder.Abcd);
        Assert.HasCount(2, regs);
        Assert.AreEqual((ushort)0x3F80, regs[0]);
        Assert.AreEqual((ushort)0x0000, regs[1]);
        Assert.AreEqual(1.0f, Float32Codec.Decode(regs, FloatWordOrder.Abcd));
    }

    [TestMethod]
    public void AllOrdersRoundTrip()
    {
        float[] samples = [0.0f, -1.5f, 3.14159f, 12345.678f, float.MaxValue];
        FloatWordOrder[] orders = [FloatWordOrder.Abcd, FloatWordOrder.Cdab, FloatWordOrder.Badc, FloatWordOrder.Dcba];
        foreach (float sample in samples)
        {
            foreach (FloatWordOrder order in orders)
            {
                Assert.AreEqual(sample, Float32Codec.Decode(Float32Codec.Encode(sample, order), order));
            }
        }
    }

    [TestMethod]
    public void WordOrdersDiffer()
    {
        ushort[] abcd = Float32Codec.Encode(1.0f, FloatWordOrder.Abcd);
        ushort[] cdab = Float32Codec.Encode(1.0f, FloatWordOrder.Cdab);
        CollectionAssert.AreEqual(new ushort[] { abcd[1], abcd[0] }, cdab);
    }

    [TestMethod]
    public void DecodeRejectsWrongLength()
    {
        ushort[] one = [0x3F80];
        Assert.ThrowsExactly<ArgumentException>(() => Float32Codec.Decode(one, FloatWordOrder.Abcd));
    }

    [TestMethod]
    public void DisplayStringsUseEndianTerms()
    {
        Assert.AreEqual("Big Endian (ABCD)", FloatWordOrderText.ToDisplayString(FloatWordOrder.Abcd));
        Assert.AreEqual("Little Endian (DCBA)", FloatWordOrderText.ToDisplayString(FloatWordOrder.Dcba));
        Assert.IsTrue(FloatWordOrderText.ToDisplayString(FloatWordOrder.Badc).Contains("Big Endian", StringComparison.Ordinal));
        Assert.IsTrue(FloatWordOrderText.ToDisplayString(FloatWordOrder.Cdab).Contains("Little Endian", StringComparison.Ordinal));
    }
}
