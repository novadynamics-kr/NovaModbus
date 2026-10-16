using NovaModbus.Rtu.Commands;
using NovaModbus.ClientWPF.Services;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Enums;

namespace NovaModbus.ClientWPF.ViewModels;

/// <summary>Single-shot function-code execution tab.</summary>
public sealed class ReadWriteViewModel : ViewModelBase
{
    private readonly Func<ModbusRtuClient?> _clientProvider;
    private ModbusFunction _selectedFunction = ModbusFunction.ReadHoldingRegisters;
    private string _slaveIdText = "1";
    private string _startText = "0";
    private string _countText = "10";
    private string _valuesText = string.Empty;
    private string _resultText = string.Empty;
    private bool _isBusy;

    /// <summary>Initializes with a live client provider (null when disconnected).</summary>
    public ReadWriteViewModel(Func<ModbusRtuClient?> clientProvider)
    {
        ArgumentNullException.ThrowIfNull(clientProvider);
        _clientProvider = clientProvider;
        ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, () => !IsBusy, OnError);
    }

    /// <summary>All supported functions.</summary>
    public IReadOnlyList<ModbusFunction> AvailableFunctions { get; } =
    [
        ModbusFunction.ReadCoils,
        ModbusFunction.ReadDiscreteInputs,
        ModbusFunction.ReadHoldingRegisters,
        ModbusFunction.ReadInputRegisters,
        ModbusFunction.WriteSingleCoil,
        ModbusFunction.WriteSingleRegister,
        ModbusFunction.WriteMultipleCoils,
        ModbusFunction.WriteMultipleRegisters,
        ModbusFunction.ReadExceptionStatus,
        ModbusFunction.Diagnostics,
        ModbusFunction.GetCommEventCounter,
        ModbusFunction.GetCommEventLog,
        ModbusFunction.ReportServerId,
        ModbusFunction.ReadFileRecord,
        ModbusFunction.WriteFileRecord,
        ModbusFunction.MaskWriteRegister,
        ModbusFunction.ReadWriteMultipleRegisters,
        ModbusFunction.ReadFifoQueue,
    ];

    /// <summary>Selected function.</summary>
    public ModbusFunction SelectedFunction
    {
        get => _selectedFunction;
        set
        {
            if (SetProperty(ref _selectedFunction, value))
            {
                NotifyPropertyChanged(nameof(NeedsCount));
                NotifyPropertyChanged(nameof(NeedsValues));
                NotifyPropertyChanged(nameof(HintText));
            }
        }
    }

    /// <summary>Slave ID text (1-247).</summary>
    public string SlaveIdText
    {
        get => _slaveIdText;
        set => SetProperty(ref _slaveIdText, value);
    }

    /// <summary>Start address text.</summary>
    public string StartText
    {
        get => _startText;
        set => SetProperty(ref _startText, value);
    }

    /// <summary>Quantity text (reads, FC15/16).</summary>
    public string CountText
    {
        get => _countText;
        set => SetProperty(ref _countText, value);
    }

    /// <summary>Write payload text (see hint per function).</summary>
    public string ValuesText
    {
        get => _valuesText;
        set => SetProperty(ref _valuesText, value);
    }

    /// <summary>Result or error output.</summary>
    public string ResultText
    {
        get => _resultText;
        private set => SetProperty(ref _resultText, value);
    }

    /// <summary>Execution in progress.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ExecuteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Whether the count field applies.</summary>
    public bool NeedsCount => SelectedFunction is
        ModbusFunction.ReadCoils or
        ModbusFunction.ReadDiscreteInputs or
        ModbusFunction.ReadHoldingRegisters or
        ModbusFunction.ReadInputRegisters or
        ModbusFunction.ReadWriteMultipleRegisters;

    /// <summary>Whether the values field applies.</summary>
    public bool NeedsValues => SelectedFunction is not
        (ModbusFunction.ReadCoils or
        ModbusFunction.ReadDiscreteInputs or
        ModbusFunction.ReadHoldingRegisters or
        ModbusFunction.ReadInputRegisters or
        ModbusFunction.ReadExceptionStatus or
        ModbusFunction.GetCommEventCounter or
        ModbusFunction.GetCommEventLog or
        ModbusFunction.ReportServerId or
        ModbusFunction.ReadFifoQueue);

    /// <summary>Usage hint for the values field.</summary>
    public string HintText => SelectedFunction switch
    {
        ModbusFunction.WriteSingleCoil => "Values: 1 or 0.",
        ModbusFunction.WriteSingleRegister => "Values: single value (e.g. 0x12).",
        ModbusFunction.WriteMultipleCoils => "Values: 1,0,1 or 101.",
        ModbusFunction.WriteMultipleRegisters => "Values: 10, 0x12, 255.",
        ModbusFunction.Diagnostics => "Start = sub-function (e.g. 0 or 0x000B). Values = data bytes (e.g. 12 34), may be empty.",
        ModbusFunction.ReadFileRecord => "Values: file,record,len; ... (e.g. 4,1,2; 4,3,1).",
        ModbusFunction.WriteFileRecord => "Values: file,record: v1 v2; ... (e.g. 4,7: 6,9).",
        ModbusFunction.MaskWriteRegister => "Start = address. Values: andMask,orMask (e.g. 0xF2,0x25).",
        ModbusFunction.ReadWriteMultipleRegisters => "Start = read address, Count = read count. Values: writeStart, v1, v2, ...",
        ModbusFunction.ReadFifoQueue => "Start = pointer address.",
        _ => "No extra input needed.",
    };

    /// <summary>Run the request.</summary>
    public AsyncRelayCommand ExecuteCommand { get; }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            await RunOnceAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Run the configured request once, updating <see cref="ResultText"/>.</summary>
    public async Task<string> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ModbusRtuClient? client = _clientProvider();
            if (client is null)
            {
                ResultText = "Not connected.";
                return ResultText;
            }

            byte slaveId = ParseSlaveId();
            ushort start = ParseUshort(StartText, "start address");
            ResultText = SelectedFunction switch
            {
                ModbusFunction.ReadCoils => ValueFormatter.FormatBits(await client.ReadCoilsAsync(slaveId, start, ParseCount(), cancellationToken).ConfigureAwait(false)),
                ModbusFunction.ReadDiscreteInputs => ValueFormatter.FormatBits(await client.ReadDiscreteInputsAsync(slaveId, start, ParseCount(), cancellationToken).ConfigureAwait(false)),
                ModbusFunction.ReadHoldingRegisters => ValueFormatter.FormatRegisters(await client.ReadHoldingRegistersAsync(slaveId, start, ParseCount(), cancellationToken).ConfigureAwait(false)),
                ModbusFunction.ReadInputRegisters => ValueFormatter.FormatRegisters(await client.ReadInputRegistersAsync(slaveId, start, ParseCount(), cancellationToken).ConfigureAwait(false)),
                ModbusFunction.WriteSingleCoil => await WriteSingleCoilAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
                ModbusFunction.WriteSingleRegister => await WriteSingleRegisterAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
                ModbusFunction.WriteMultipleCoils => await WriteMultipleCoilsAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
                ModbusFunction.WriteMultipleRegisters => await WriteMultipleRegistersAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
                ModbusFunction.ReadExceptionStatus => await ReadExceptionStatusAsync(client, slaveId, cancellationToken).ConfigureAwait(false),
                ModbusFunction.Diagnostics => await DiagnosticsAsync(client, slaveId, cancellationToken).ConfigureAwait(false),
                ModbusFunction.GetCommEventCounter => await CommEventCounterAsync(client, slaveId, cancellationToken).ConfigureAwait(false),
                ModbusFunction.GetCommEventLog => await CommEventLogAsync(client, slaveId, cancellationToken).ConfigureAwait(false),
                ModbusFunction.ReportServerId => await ReportServerIdAsync(client, slaveId, cancellationToken).ConfigureAwait(false),
                ModbusFunction.ReadFileRecord => await ReadFileRecordAsync(client, slaveId, cancellationToken).ConfigureAwait(false),
                ModbusFunction.WriteFileRecord => await WriteFileRecordAsync(client, slaveId, cancellationToken).ConfigureAwait(false),
                ModbusFunction.MaskWriteRegister => await MaskWriteAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
                ModbusFunction.ReadWriteMultipleRegisters => await ReadWriteMultipleAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
                ModbusFunction.ReadFifoQueue => await ReadFifoAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
                _ => throw new InvalidOperationException($"Unsupported function {SelectedFunction}."),
            };
        }
        catch (Exception ex)
        {
            ResultText = $"Error: {ex.Message}";
        }

        return ResultText;
    }

    private async Task<string> WriteSingleCoilAsync(ModbusRtuClient client, byte slaveId, ushort start, CancellationToken ct)
    {
        bool on = ParseSingleBit();
        await client.WriteSingleCoilAsync(slaveId, start, on, ct).ConfigureAwait(false);
        return $"OK: coil {start} <- {(on ? 1 : 0)}";
    }

    private async Task<string> WriteSingleRegisterAsync(ModbusRtuClient client, byte slaveId, ushort start, CancellationToken ct)
    {
        ushort value = ParseSingleRegister();
        await client.WriteSingleRegisterAsync(slaveId, start, value, ct).ConfigureAwait(false);
        return $"OK: reg {start} <- 0x{value:X4} ({value})";
    }

    private async Task<string> WriteMultipleCoilsAsync(ModbusRtuClient client, byte slaveId, ushort start, CancellationToken ct)
    {
        bool[] values = ParseBitList();
        await client.WriteMultipleCoilsAsync(slaveId, start, values, ct).ConfigureAwait(false);
        return $"OK: {values.Length} coils @ {start}";
    }

    private async Task<string> WriteMultipleRegistersAsync(ModbusRtuClient client, byte slaveId, ushort start, CancellationToken ct)
    {
        ushort[] values = ParseRegisterList();
        await client.WriteMultipleRegistersAsync(slaveId, start, values, ct).ConfigureAwait(false);
        return $"OK: {values.Length} regs @ {start}";
    }

    private static async Task<string> ReadExceptionStatusAsync(ModbusRtuClient client, byte slaveId, CancellationToken ct)
    {
        byte status = await client.ReadExceptionStatusAsync(slaveId, ct).ConfigureAwait(false);
        return $"Status: 0x{status:X2} ({status})";
    }

    private async Task<string> DiagnosticsAsync(ModbusRtuClient client, byte slaveId, CancellationToken ct)
    {
        ushort sub = ParseUshortHex(StartText, "sub-function");
        byte[] data = ParseByteList();
        byte[] echo = await client.DiagnosticsAsync(slaveId, sub, data, ct).ConfigureAwait(false);
        return $"Sub 0x{sub:X4} echo: {Hex.Format(echo)}";
    }

    private static async Task<string> CommEventCounterAsync(ModbusRtuClient client, byte slaveId, CancellationToken ct)
    {
        (ushort status, ushort events) = await client.GetCommEventCounterAsync(slaveId, ct).ConfigureAwait(false);
        return $"Status: 0x{status:X4}, Events: {events}";
    }

    private static async Task<string> CommEventLogAsync(ModbusRtuClient client, byte slaveId, CancellationToken ct)
    {
        (ushort status, ushort events, ushort messages, byte[] log) = await client.GetCommEventLogAsync(slaveId, ct).ConfigureAwait(false);
        return $"Status: 0x{status:X4}, Events: {events}, Messages: {messages}{Environment.NewLine}{Hex.Format(log)}";
    }

    private static async Task<string> ReportServerIdAsync(ModbusRtuClient client, byte slaveId, CancellationToken ct)
    {
        (byte[] id, bool running) = await client.ReportServerIdAsync(slaveId, ct).ConfigureAwait(false);
        return $"ID: {ValueFormatter.FormatDeviceId(id)}, Running: {running}";
    }

    private async Task<string> ReadFileRecordAsync(ModbusRtuClient client, byte slaveId, CancellationToken ct)
    {
        FileSubRequest[] subs = ParseFileReadSubs();
        ushort[][] records = await client.ReadFileRecordAsync(slaveId, subs, ct).ConfigureAwait(false);
        List<string> lines = new(records.Length);
        for (int i = 0; i < records.Length; i++)
        {
            lines.Add($"File {subs[i].FileNumber} rec {subs[i].RecordNumber}:{Environment.NewLine}{ValueFormatter.FormatRegisters(records[i])}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private async Task<string> WriteFileRecordAsync(ModbusRtuClient client, byte slaveId, CancellationToken ct)
    {
        (FileSubRequest[] subs, ushort[][] datas) = ParseFileWriteSubs();
        await client.WriteFileRecordAsync(slaveId, subs, datas, ct).ConfigureAwait(false);
        return $"OK: {subs.Length} file writes";
    }

    private async Task<string> MaskWriteAsync(ModbusRtuClient client, byte slaveId, ushort address, CancellationToken ct)
    {
        (ushort andMask, ushort orMask) = ParseMaskPair();
        await client.MaskWriteRegisterAsync(slaveId, address, andMask, orMask, ct).ConfigureAwait(false);
        return $"OK: reg {address} masked (AND 0x{andMask:X4}, OR 0x{orMask:X4})";
    }

    private async Task<string> ReadWriteMultipleAsync(ModbusRtuClient client, byte slaveId, ushort readStart, CancellationToken ct)
    {
        (ushort writeStart, ushort[] writeValues) = ParseWriteStartValues();
        ushort[] read = await client.ReadWriteMultipleRegistersAsync(
            slaveId, readStart, ParseCount(), writeStart, writeValues, ct).ConfigureAwait(false);
        return ValueFormatter.FormatRegisters(read);
    }

    private static async Task<string> ReadFifoAsync(ModbusRtuClient client, byte slaveId, ushort pointer, CancellationToken ct)
    {
        ushort[] values = await client.ReadFifoQueueAsync(slaveId, pointer, ct).ConfigureAwait(false);
        return values.Length == 0 ? "(empty queue)" : ValueFormatter.FormatRegisters(values);
    }

    private void OnError(Exception ex) => ResultText = $"Error: {ex.Message}";

    private byte ParseSlaveId()
    {
        if (!byte.TryParse(SlaveIdText, out byte id) || id is 0 or > 247)
        {
            throw new ArgumentException($"Invalid slave ID: '{SlaveIdText}' (1-247).");
        }

        return id;
    }

    private ushort ParseCount()
    {
        if (!ushort.TryParse(CountText, out ushort count) || count == 0)
        {
            throw new ArgumentException($"Invalid count: '{CountText}'.");
        }

        return count;
    }

    private static ushort ParseUshort(string text, string name)
    {
        if (!ushort.TryParse(text, out ushort value))
        {
            throw new ArgumentException($"Invalid {name}: '{text}'.");
        }

        return value;
    }

    private bool ParseSingleBit() => ParseBitList()[0];

    private ushort ParseSingleRegister() => ParseRegisterList()[0];

    private bool[] ParseBitList()
    {
        string[] tokens = ValuesText.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0 && ValuesText.Trim().Length != 0)
        {
            // Compact form like "10101".
            string compact = ValuesText.Trim();
            bool[] bits = new bool[compact.Length];
            for (int i = 0; i < compact.Length; i++)
            {
                bits[i] = compact[i] switch
                {
                    '1' => true,
                    '0' => false,
                    _ => throw new ArgumentException($"Invalid bit '{compact[i]}' (use 0/1)."),
                };
            }

            return bits;
        }

        if (tokens.Length == 0)
        {
            throw new ArgumentException("Enter coil values (e.g. 1,0,1 or 101).");
        }

        bool[] values = new bool[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            values[i] = tokens[i].Trim() switch
            {
                "1" or "true" => true,
                "0" or "false" => false,
                _ => throw new ArgumentException($"Invalid bit '{tokens[i]}' (use 0/1)."),
            };
        }

        return values;
    }

    private ushort[] ParseRegisterList()
    {
        string[] tokens = ValuesText.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw new ArgumentException("Enter register values (e.g. 10, 0x12, 255).");
        }

        ushort[] values = new ushort[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            values[i] = ParseUshortToken(tokens[i]);
        }

        return values;
    }

    private static ushort ParseUshortToken(string token)
    {
        string text = token.Trim();
        try
        {
            return text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToUInt16(text[2..], 16)
                : ushort.Parse(text, null);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new ArgumentException($"Invalid value '{token}'.", ex);
        }
    }

    private static ushort ParseUshortHex(string text, string name)
    {
        try
        {
            return ParseUshortToken(text);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Invalid {name}: '{text}'.");
        }
    }

    private byte[] ParseByteList()
    {
        string[] tokens = ValuesText.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries);
        byte[] data = new byte[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            string text = tokens[i].Trim();
            try
            {
                data[i] = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToByte(text[2..], 16)
                    : byte.Parse(text, null);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new ArgumentException($"Invalid byte '{tokens[i]}'.", ex);
            }
        }

        return data;
    }

    private FileSubRequest[] ParseFileReadSubs()
    {
        string[] groups = ValuesText.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (groups.Length == 0)
        {
            throw new ArgumentException("Enter sub-requests (e.g. 4,1,2; 4,3,1).");
        }

        FileSubRequest[] subs = new FileSubRequest[groups.Length];
        for (int i = 0; i < groups.Length; i++)
        {
            string[] parts = groups[i].Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                throw new ArgumentException($"Invalid sub-request '{groups[i].Trim()}' (file,record,len).");
            }

            subs[i] = new FileSubRequest(ParseUshortToken(parts[0]), ParseUshortToken(parts[1]), ParseUshortToken(parts[2]));
        }

        return subs;
    }

    private (FileSubRequest[] Subs, ushort[][] Datas) ParseFileWriteSubs()
    {
        string[] groups = ValuesText.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (groups.Length == 0)
        {
            throw new ArgumentException("Enter writes (e.g. 4,7: 6,9).");
        }

        FileSubRequest[] subs = new FileSubRequest[groups.Length];
        ushort[][] datas = new ushort[groups.Length][];
        for (int i = 0; i < groups.Length; i++)
        {
            string[] halves = groups[i].Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (halves.Length != 2)
            {
                throw new ArgumentException($"Invalid write '{groups[i].Trim()}' (file,record: values).");
            }

            string[] head = halves[0].Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (head.Length != 2)
            {
                throw new ArgumentException($"Invalid write '{groups[i].Trim()}' (file,record: values).");
            }

            string[] valueTokens = halves[1].Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
            if (valueTokens.Length == 0)
            {
                throw new ArgumentException($"Missing values in '{groups[i].Trim()}'.");
            }

            ushort[] data = new ushort[valueTokens.Length];
            for (int j = 0; j < valueTokens.Length; j++)
            {
                data[j] = ParseUshortToken(valueTokens[j]);
            }

            subs[i] = new FileSubRequest(ParseUshortToken(head[0]), ParseUshortToken(head[1]), (ushort)data.Length);
            datas[i] = data;
        }

        return (subs, datas);
    }

    private (ushort AndMask, ushort OrMask) ParseMaskPair()
    {
        ushort[] values = ParseRegisterList();
        if (values.Length != 2)
        {
            throw new ArgumentException("Enter two masks (e.g. 0xF2,0x25).");
        }

        return (values[0], values[1]);
    }

    private (ushort WriteStart, ushort[] WriteValues) ParseWriteStartValues()
    {
        ushort[] values = ParseRegisterList();
        if (values.Length < 2)
        {
            throw new ArgumentException("Enter writeStart then values (e.g. 10, 255, 238).");
        }

        return (values[0], values[1..]);
    }
}
