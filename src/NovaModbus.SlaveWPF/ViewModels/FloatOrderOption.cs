using NovaModbus.Rtu.Codecs;

namespace NovaModbus.SlaveWPF.ViewModels;

/// <summary>Float word order choice with an Endian display name.</summary>
/// <param name="Order">Codec order.</param>
/// <param name="Display">UI text from <see cref="FloatWordOrderText"/>.</param>
public sealed record FloatOrderOption(FloatWordOrder Order, string Display)
{
    /// <summary>All options in stable order.</summary>
    public static IReadOnlyList<FloatOrderOption> All { get; } =
    [
        new(FloatWordOrder.Abcd, FloatWordOrderText.ToDisplayString(FloatWordOrder.Abcd)),
        new(FloatWordOrder.Badc, FloatWordOrderText.ToDisplayString(FloatWordOrder.Badc)),
        new(FloatWordOrder.Cdab, FloatWordOrderText.ToDisplayString(FloatWordOrder.Cdab)),
        new(FloatWordOrder.Dcba, FloatWordOrderText.ToDisplayString(FloatWordOrder.Dcba)),
    ];
}
