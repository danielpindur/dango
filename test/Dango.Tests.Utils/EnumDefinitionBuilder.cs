namespace Dango.Tests.Utils;

public static class EnumDefinitionBuilder
{
    public static string BuildEnumDefinition(string enumName, int valueCount, string? namespaceName = null)
    {
        var values = Enumerable.Range(0, valueCount)
            .Select(i => $"Value{i}")
            .ToList();

        var namespaceDeclaration = string.IsNullOrEmpty(namespaceName)
            ? ""
            : $"namespace {namespaceName};\n\n";

        var enumValues = string.Join(",\n        ", values);

        return $@"{namespaceDeclaration}public enum {enumName}
{{
        {enumValues}
}}";
    }

    public static string BuildEnumDefinitionWithValues(string enumName, IEnumerable<string> values, string? namespaceName = null)
    {
        var namespaceDeclaration = string.IsNullOrEmpty(namespaceName)
            ? ""
            : $"namespace {namespaceName};\n\n";

        var enumValues = string.Join(",\n        ", values);

        return $@"{namespaceDeclaration}public enum {enumName}
{{
        {enumValues}
}}";
    }
}
