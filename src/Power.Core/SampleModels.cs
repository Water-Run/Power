namespace Power.Core;

public static class SampleModels
{
    public const ulong VoltageChannel = 100;

    /// <summary>Synthetic parameters only. Shared by Unity Studio and headless verification.</summary>
    public static ModelDefinition Electrothermal(ulong stepNanoseconds = 100_000) => new()
    {
        StepNanoseconds = stepNanoseconds,
        Nodes = [NodeDefinition.Rotor(1, 0.2), NodeDefinition.Rotor(2, 1),
                 NodeDefinition.Thermal(3, 100, 300), NodeDefinition.Thermal(4, 200, 300)],
        Components = [ComponentDefinition.Motor(10, 1, 3, VoltageChannel, 0.5, 0.02, 0.8, 24),
            ComponentDefinition.Shaft(11, 1, 2, 100, 1, 3, 4),
            ComponentDefinition.Shaft(12, 2, 0, 0, 0.1),
            ComponentDefinition.ThermalLink(13, 3, 4, 5),
            ComponentDefinition.ThermalLink(14, 4, 0, 2, 300)]
    };
}
