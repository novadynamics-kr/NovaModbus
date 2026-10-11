using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Enums;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class CodecTests
{
    [TestMethod]
    public void ReadBitsRequestRoundTrip()
    {
        byte[] body = ReadBitsCodec.EncodeRequest(0x0013, 19);
        ReadBitsCodec.DecodeRequest(body, out ushort start, out ushort count);
        Assert.AreEqual((ushort)0x0013, start);
        Assert.AreEqual((ushort)19, count);
    }

    [TestMethod]
    public void ReadBitsResponseRoundTripWithPaddingCheck()
    {
        bool[] values = [true, false, true, true, false, false, false, false, true];
        byte[] body = ReadBitsCodec.EncodeResponse(values);
        Assert.AreEqual((byte)2, body[0]);
        bool[] decoded = ReadBitsCodec.DecodeResponse(body, values.Length);
        CollectionAssert.AreEqual(values, decoded);
    }

    [TestMethod]
    public void ReadBitsZeroCountRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ReadBitsCodec.EncodeRequest(0, 0));
    }

    [TestMethod]
    public void ReadBitsOverLimitRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ReadBitsCodec.EncodeRequest(0, 2001));
    }

    [TestMethod]
    public void ReadRegistersRequestRoundTrip()
    {
        byte[] body = ReadRegistersCodec.EncodeRequest(0x006B, 3);
        ReadRegistersCodec.DecodeRequest(body, out ushort start, out ushort count);
        Assert.AreEqual((ushort)0x006B, start);
        Assert.AreEqual((ushort)3, count);
    }

    [TestMethod]
    public void ReadRegistersResponseRoundTripBigEndian()
    {
        ushort[] values = [0x022B, 0x0000, 0x0064];
        byte[] body = ReadRegistersCodec.EncodeResponse(values);
        Assert.AreEqual((byte)6, body[0]);
        CollectionAssert.AreEqual(values, ReadRegistersCodec.DecodeResponse(body));
    }

    [TestMethod]
    public void ReadRegistersOverLimitRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ReadRegistersCodec.EncodeRequest(0, 126));
    }

    [TestMethod]
    public void WriteSingleCoilRoundTripOn()
    {
        byte[] body = WriteSingleCoilCodec.Encode(0x00AC, true);
        WriteSingleCoilCodec.Decode(body, out ushort address, out bool on);
        Assert.AreEqual((ushort)0x00AC, address);
        Assert.IsTrue(on);
    }

    [TestMethod]
    public void WriteSingleCoilRejectsIllegalValue()
    {
        byte[] body = [0x00, 0xAC, 0x12, 0x34];
        Assert.ThrowsExactly<ArgumentException>(() => WriteSingleCoilCodec.Decode(body, out _, out _));
    }

    [TestMethod]
    public void WriteSingleRegisterRoundTrip()
    {
        byte[] body = WriteSingleRegisterCodec.Encode(0x0001, 0x0003);
        WriteSingleRegisterCodec.Decode(body, out ushort address, out ushort value);
        Assert.AreEqual((ushort)0x0001, address);
        Assert.AreEqual((ushort)0x0003, value);
    }

    [TestMethod]
    public void WriteMultipleCoilsRoundTrip()
    {
        bool[] values = [true, false, true, false, true, true, false, true, false, true];
        byte[] body = WriteMultipleCoilsCodec.EncodeRequest(0x0013, values);
        WriteMultipleCoilsCodec.DecodeRequest(body, out ushort start, out bool[] decoded);
        Assert.AreEqual((ushort)0x0013, start);
        CollectionAssert.AreEqual(values, decoded);

        byte[] resp = WriteMultipleCoilsCodec.EncodeResponse(0x0013, (ushort)values.Length);
        WriteMultipleCoilsCodec.DecodeResponse(resp, out ushort rs, out ushort rc);
        Assert.AreEqual((ushort)0x0013, rs);
        Assert.AreEqual((ushort)values.Length, rc);
    }

    [TestMethod]
    public void WriteMultipleCoilsByteCountMismatchRejected()
    {
        bool[] seed = [true, false, true];
        byte[] body = WriteMultipleCoilsCodec.EncodeRequest(0, seed);
        body[4] = 99;
        Assert.ThrowsExactly<ArgumentException>(() => WriteMultipleCoilsCodec.DecodeRequest(body, out _, out _));
    }

    [TestMethod]
    public void WriteMultipleRegistersRoundTrip()
    {
        ushort[] values = [0x000A, 0x0102];
        byte[] body = WriteMultipleRegistersCodec.EncodeRequest(0x0001, values);
        WriteMultipleRegistersCodec.DecodeRequest(body, out ushort start, out ushort[] decoded);
        Assert.AreEqual((ushort)0x0001, start);
        CollectionAssert.AreEqual(values, decoded);

        byte[] resp = WriteMultipleRegistersCodec.EncodeResponse(0x0001, (ushort)values.Length);
        WriteMultipleRegistersCodec.DecodeResponse(resp, out ushort rs, out ushort rc);
        Assert.AreEqual((ushort)0x0001, rs);
        Assert.AreEqual((ushort)2, rc);
    }

    [TestMethod]
    public void WriteMultipleRegistersByteCountMismatchRejected()
    {
        ushort[] seed = [1, 2];
        byte[] body = WriteMultipleRegistersCodec.EncodeRequest(0, seed);
        body[4] = 3;
        Assert.ThrowsExactly<ArgumentException>(() => WriteMultipleRegistersCodec.DecodeRequest(body, out _, out _));
    }

    [TestMethod]
    public void ExceptionCodecRoundTrip()
    {
        byte[] body = ExceptionCodec.Encode(ModbusExceptionCode.IllegalDataAddress);
        Assert.AreEqual(ModbusExceptionCode.IllegalDataAddress, ExceptionCodec.Decode(body));
        Assert.AreEqual((byte)0x83, ExceptionCodec.ExceptionFunction(ModbusFunction.ReadHoldingRegisters));
    }

    [TestMethod]
    public void ExceptionFrameFlagDetected()
    {
        byte[] payload = [0x02];
        ModbusFrame frame = new(0x01, 0x83, payload);
        Assert.IsTrue(frame.IsException);
        Assert.AreEqual(ModbusFunction.ReadHoldingRegisters, frame.BaseFunction);
    }
}
