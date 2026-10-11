using NovaModbus.Client.Services;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Enums;

namespace NovaModbus.Client.ViewModels;

/// <summary>Single-shot function-code execution tab.</summary>
public sealed class ReadWriteViewModel : ViewModelBase
{
    private static readonly ModbusFunction[] ReadBitsFunctions =
        [ModbusFunction.ReadCoils, ModbusFunction.ReadDiscreteInputs];
    private static readonly ModbusFunction[] ReadRegisterFunctions =
        [ModbusFunction.ReadHoldingRegisters, ModbusFunction.ReadInputRegisters];

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

    /// <summary>All phase-1 functions.</summary>
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
    public bool NeedsCount => !IsSingleWrite;

    /// <summary>Whether the values field applies.</summary>
    public bool NeedsValues => !IsRead;

    private bool IsRead => ReadBitsFunctions.Contains(SelectedFunction) || ReadRegisterFunctions.Contains(SelectedFunction);

    private bool IsSingleWrite => SelectedFunction is ModbusFunction.WriteSingleCoil or ModbusFunction.WriteSingleRegister;

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
                ModbusFunction.ReadCoils => FormatBits(await client.ReadCoilsAsync(slaveId, start, ParseCount(), cancellationToken).ConfigureAwait(false)),
                ModbusFunction.ReadDiscreteInputs => FormatBits(await client.ReadDiscreteInputsAsync(slaveId, start, ParseCount(), cancellationToken).ConfigureAwait(false)),
                ModbusFunction.ReadHoldingRegisters => FormatRegisters(await client.ReadHoldingRegistersAsync(slaveId, start, ParseCount(), cancellationToken).ConfigureAwait(false)),
                ModbusFunction.ReadInputRegisters => FormatRegisters(await client.ReadInputRegistersAsync(slaveId, start, ParseCount(), cancellationToken).ConfigureAwait(false)),
                ModbusFunction.WriteSingleCoil => await WriteSingleCoilAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
                ModbusFunction.WriteSingleRegister => await WriteSingleRegisterAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
                ModbusFunction.WriteMultipleCoils => await WriteMultipleCoilsAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
                ModbusFunction.WriteMultipleRegisters => await WriteMultipleRegistersAsync(client, slaveId, start, cancellationToken).ConfigureAwait(false),
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
            string token = tokens[i].Trim();
            try
            {
                values[i] = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToUInt16(token[2..], 16)
                    : ushort.Parse(token, null);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new ArgumentException($"Invalid register '{tokens[i]}'.", ex);
            }
        }

        return values;
    }

    private static string FormatBits(bool[] values) =>
        string.Join(' ', values.Select(b => b ? "1" : "0"));

    private static string FormatRegisters(ushort[] values) =>
        string.Join(Environment.NewLine, values.Select((v, i) => $"[{i}] 0x{v:X4} ({v})"));
}
