using Microsoft.CodeAnalysis;

namespace Dango.Models;

internal sealed class EnumPair : IEquatable<EnumPair>
{
    private readonly int _hashCode;

    public EnumPair(
        INamedTypeSymbol sourceEnum,
        INamedTypeSymbol destinationEnum
    )
    {
        SourceEnum = sourceEnum;
        DestinationEnum = destinationEnum;

        unchecked
        {
            _hashCode =
                StringComparer.Ordinal.GetHashCode(
                    SourceEnum.ToDisplayString()
                )
                * StringComparer.Ordinal.GetHashCode(
                    DestinationEnum.ToDisplayString()
                );
        }
    }

    public INamedTypeSymbol SourceEnum { get; }

    public INamedTypeSymbol DestinationEnum { get; }

    public override bool Equals(object? obj)
    {
        if (obj is not EnumPair other)
        {
            return false;
        }

        return Equals(other);
    }

    public bool Equals(EnumPair? other)
    {
        if (other is null)
        {
            return false;
        }

        return other._hashCode == _hashCode;
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }
}