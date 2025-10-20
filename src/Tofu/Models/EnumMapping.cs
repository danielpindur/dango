namespace Tofu.Models;

internal sealed class EnumMapping
{
    public MappingStrategy Strategy { get; set; }

    public string? DefaultValue { get; set; }

    public Dictionary<string, string>? Overrides { get; set; }
}