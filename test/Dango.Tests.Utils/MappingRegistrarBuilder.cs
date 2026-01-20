using System.Text;

namespace Dango.Tests.Utils;

public static class MappingRegistrarBuilder
{
    public static string BuildRegistrar(
        IEnumerable<(string SourceEnum, string DestinationEnum, MappingConfig Config)> mappings,
        string? namespaceName = null,
        string registrarClassName = "MyRegistrar")
    {
        var namespaceDeclaration = string.IsNullOrEmpty(namespaceName)
            ? ""
            : $"namespace {namespaceName};\n\n";

        var sb = new StringBuilder();

        sb.AppendLine("using Dango.Abstractions;");
        sb.AppendLine("using System.Collections.Generic;");

        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        sb.AppendLine($"public class {registrarClassName} : IDangoMapperRegistrar");
        sb.AppendLine("{");
        sb.AppendLine("    public void Register(IDangoMapperRegistry registry)");
        sb.AppendLine("    {");

        foreach (var (sourceEnum, destinationEnum, config) in mappings)
        {
            sb.Append($"        registry.Enum<{sourceEnum}, {destinationEnum}>()");

            if (config.Strategy == MappingStrategy.ByValue)
            {
                sb.Append(".MapByValue()");
            }
            else if (config.Strategy == MappingStrategy.ByName)
            {
                sb.Append(".MapByName()");
            }

            if (config.DefaultValue != null)
            {
                sb.Append($".WithDefault({destinationEnum}.{config.DefaultValue})");
            }

            if (config.Overrides != null && config.Overrides.Count > 0)
            {
                sb.AppendLine("            .WithOverrides(new Dictionary<");
                sb.Append($"{sourceEnum}, {destinationEnum}>");
                sb.AppendLine(" {");

                foreach (var (source, dest) in config.Overrides)
                {
                    sb.AppendLine($"                {{ {sourceEnum}.{source}, {destinationEnum}.{dest} }},");
                }

                sb.Append("            })");
            }

            sb.AppendLine(";");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}

public class MappingConfig
{
    public MappingStrategy Strategy { get; set; } = MappingStrategy.ByName;
    public string? DefaultValue { get; set; }
    public Dictionary<string, string>? Overrides { get; set; }
}

public enum MappingStrategy
{
    ByName,
    ByValue
}
