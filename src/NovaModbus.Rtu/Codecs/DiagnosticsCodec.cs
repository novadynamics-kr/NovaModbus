using System.Buffers.Binary;

namespace NovaModbus.Rtu.Codecs;

/// <summary>FC08 Diagnostics: [sub-function BE(2)][data...] both ways.</summary>
public static class DiagnosticsCodec
{
    /// <summary>Well-known sub-functions.</summary>
    public static class Subfunctions
    {
        /// <summary>Return query data (echo).</summary>
        public const ushort ReturnQueryData = 0x0000;

        /// <summary>Restart communications option.</summary>
        public const ushort RestartCommunicationsOption = 0x0001;

        /// <summary>Return diagnostic register.</summary>
        public const ushort ReturnDiagnosticRegister = 0x0002;

        /// <summary>Force listen-only mode (no response).</summary>
        public const ushort ForceListenOnlyMode = 0x0004;

        /// <summary>Clear counters and diagnostic register.</summary>
        public const ushort ClearCounters = 0x000A;

        /// <summary>Return bus message count.</summary>
        public const ushort ReturnBusMessageCount = 0x000B;

        /// <summary>Return bus communication error count.</summary>
        public const ushort ReturnBusCommErrorCount = 0x000C;

        /// <summary>Return slave exception error count.</summary>
        public const ushort ReturnSlaveExceptionCount = 0x000D;

        /// <summary>Return slave message count.</summary>
        public const ushort ReturnSlaveMessageCount = 0x000E;

        /// <summary>Return slave no-response count.</summary>
        public const ushort ReturnSlaveNoResponseCount = 0x000F;

        /// <summary>Return slave NAK count.</summary>
        public const ushort ReturnSlaveNakCount = 0x0010;

        /// <summary>Return slave busy count.</summary>
        public const ushort ReturnSlaveBusyCount = 0x0011;

        /// <summary>Return bus character overrun count.</summary>
        public const ushort ReturnBusOverrunCount = 0x0012;

        /// <summary>Clear overrun counter and flag (echoes the request data).</summary>
        public const ushort ClearOverrunCounter = 0x0014;
    }

    /// <summary>Encode body: [sub-function BE(2)][data...].</summary>
    public static byte[] Encode(ushort subfunction, ReadOnlySpan<byte> data)
    {
        byte[] body = new byte[2 + data.Length];
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), subfunction);
        data.CopyTo(body.AsSpan(2));
        return body;
    }

    /// <summary>Decode body into sub-function and data.</summary>
    public static void Decode(ReadOnlySpan<byte> body, out ushort subfunction, out byte[] data)
    {
        if (body.Length < 2)
        {
            throw new ArgumentException("FC08 body must be at least 2 bytes.", nameof(body));
        }

        subfunction = BinaryPrimitives.ReadUInt16BigEndian(body[..2]);
        data = body[2..].ToArray();
    }

    /// <summary>Encode a two-byte counter response payload (after the sub-function).</summary>
    public static byte[] CounterData(ushort value)
    {
        byte[] data = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(data, value);
        return data;
    }
}
