using Microsoft.CodeAnalysis;

namespace Tofu.CodeGeneration;

internal static class NamingHelper
{
    public static string GenerateExtensionsClassName(INamedTypeSymbol sourceEnum)
    {
        var enumNamespace = sourceEnum.ContainingNamespace.ToDisplayString();
        var enumAssembly = sourceEnum.ContainingAssembly.ToDisplayString();

        var namePrefix = string.Empty;

        if (enumNamespace.Length > 0)
        {
            namePrefix = enumNamespace.Replace('.', '_');
        }
        else if (enumAssembly.Length > 0)
        {
            namePrefix = enumAssembly.Replace('.', '_');
        }

        return namePrefix.Length == 0 ? $"{sourceEnum.Name}Extensions" : $"{namePrefix}_{sourceEnum.Name}Extensions";
    }

    public static string GenerateExtensionMethodName(INamedTypeSymbol sourceEnum, INamedTypeSymbol destinationEnum)
    {
        if (string.Equals(sourceEnum.Name, destinationEnum.Name, StringComparison.Ordinal))
        {
            var prefix = GetFirstNamespaceDifference(sourceEnum, destinationEnum);
            return $"To{prefix}{destinationEnum.Name}";
        }

        return $"To{destinationEnum.Name}";
    }

    private static string GetFirstNamespaceDifference(INamedTypeSymbol sourceEnum, INamedTypeSymbol destinationEnum)
    {
        var sourceNamespace = sourceEnum.ContainingNamespace.ToDisplayString();
        var destinationNamespace = destinationEnum.ContainingNamespace.ToDisplayString();

        var sourceParts = sourceNamespace.Split('.');
        var destinationParts = destinationNamespace.Split('.');

        for (int i = 0; i < destinationParts.Length; i++)
        {
            if (i > sourceParts.Length - 1)
            {
                return destinationParts[i];
            }

            if (!string.Equals(sourceParts[i], destinationParts[i], StringComparison.Ordinal))
            {
                return destinationParts[i];
            }
        }

        return string.Empty;
    }
}