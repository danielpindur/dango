using System.Text;

namespace Dango.Tests.Utils;

public static class EnumDefinitionBuilder
{
    public static string BuildEnumDefinition(string enumName, int valueCount, string? namespaceName = null)
    {
        var values = Enumerable.Range(0, valueCount)
            .Select(i => $"Value{i}")
            .ToList();

        return BuildEnumDefinitionWithValues(enumName, values, namespaceName);
    }

    public static string BuildEnumDefinitionWithValues(string enumName, IEnumerable<string> values, string? namespaceName = null)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        sb.AppendLine($"public enum {enumName}");
        sb.AppendLine("{");
        foreach (var value in values)
        {
            sb.AppendLine($"    {value},");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }
}
