using NovaModbus.Rtu.Codecs;

namespace NovaModbus.Slave.ViewModels;

/// <summary>Float word order choice with an Endian display name.</summary>
/// <param name="Order">Codec order.</param>
/// <param name="Display">UI text from <see cref="FloatWordOrderText"/>.</param>
public sealed record FloatOrderOption(FloatWordOrder Order, string Display);
