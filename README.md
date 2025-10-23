# Dango - Compile-Time Safe Enum Mapping for C#

[![CI Status](https://github.com/danielpindur/dango/actions/workflows/release-please.yml/badge.svg)](https://github.com/danielpindur/dango/actions/workflows/dotnet-ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Dango.svg?style=flat-square)](https://www.nuget.org/packages/Dango/)

## Why?

Currently the most used C# mapping library is [AutoMapper](https://automapper.org/), which I am not a big fan of, because it shifts potential errors to runtime instead of compile time. This can be a problem, especially in large projects, where a simple refactoring change can break the mapping without the developer noticing it until runtime.

The problem becomes even more apparent when it comes to mapping enums. If a new value is added to the source enum, there is no compile-time check to ensure that the corresponding value is added to the destination enum as well, leading to potential runtime mapping errors.

You can solve this problem for everything except enums by simply not using AutoMapper and writing the mapping code manually, but that doesn't work for enums as using a switch statement doesn't solve the problem—there is no compile-time check to ensure that all enum values are handled and no values are missing from the destination enum.

## What?

Dango is a source generator that generates mapping code for enums at compile time, ensuring that all enum values are handled and mapped correctly. If a new value is added to the source enum, the developer will get a compile-time error if the corresponding value is not added to the destination enum as well.

Dango is designed to be simple and easy to use, with minimal configuration required and allows several options to customize the mapping behavior.

## Features

- **Compile-Time Safety**: Get errors at compile time, not runtime
- **Multiple Mapping Strategies**: Map by name or by value
- **Default Values**: Specify a default destination value for unmapped source values
- **Custom Overrides**: Override specific mappings when needed
- **Multiple Destinations**: Map a single source enum to multiple destination enums
- **Nullable Support**: Automatic generation of nullable variants for all mappings
- **Clean Generated Code**: Extension methods with switch expressions for optimal performance

## Installation

```bash
dotnet add package Dango
```

## Usage

### Basic Setup

Create a registrar class implementing `IDangoMapperRegistrar`:

```csharp
using Dango.Abstractions;

public class MyRegistrar : IDangoMapperRegistrar
{
    public void Register(IDangoMapperRegistry registry)
    {
        // Basic mapping (by name)
        registry.Enum<SourceStatus, DestinationStatus>();
        
        // Map by value instead of name
        registry.Enum<SourcePriority, DestinationPriority>().MapByValue();
        
        // Provide default for unmapped values
        registry.Enum<SourceState, DestinationState>()
            .WithDefault(DestinationState.Unknown);
        
        // Override specific mappings
        registry.Enum<SourceRole, DestinationRole>()
            .WithOverrides(new Dictionary<SourceRole, DestinationRole>
            {
                { SourceRole.Admin, DestinationRole.Administrator }
            });
        
        // Combine options
        registry.Enum<SourceType, DestinationType>()
            .MapByValue()
            .WithDefault(DestinationType.Other)
            .WithOverrides(new Dictionary<SourceType, DestinationType>
            {
                { SourceType.Special, DestinationType.Custom }
            });
        
        // Map to multiple destinations
        registry.Enum<ApiStatus, DatabaseStatus>();
        registry.Enum<ApiStatus, DisplayStatus>();
    }
}
```

### Generated Code

Dango generates extension methods for each source enum:

```csharp
// Non-nullable extension
DestinationStatus status = sourceStatus.ToDestinationStatus();

// Nullable extension (generated automatically)
DestinationStatus? nullableStatus = nullableSource.ToDestinationStatus();
```

### Handling Same Enum Names

When enums have the same name in different namespaces, Dango adds namespace prefixes:

```csharp
// Api.Status -> Database.Status generates:
var dbStatus = apiStatus.ToDatabaseStatus();
```

## Error Handling

Dango provides compile-time diagnostics for:
- Invalid enum types
- Duplicate mappings
- Unmapped source values without defaults

## Generated Code Location

All generated code is placed in `{AssemblyName}.Generated.Dango.Mappings` namespace.
