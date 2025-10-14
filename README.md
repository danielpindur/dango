# Tofu - Compile Time Safe Enum Mapping for C#

## Why?
Currently the most used C# mapping library is [AutoMapper](https://automapper.org/), which I am not a big fan of, because it shifts the potential errors to runtime, instead of compile time. This can be a problem, especially in large projects, where a simple refactoring change can break the mapping without the developer noticing it until runtime.

The problem becomes even more apparent when it comes to mapping enums, where if a new value is added to the source enum, there is no compile time check to ensure that the corresponding value is added to the destination enum as well, thus leading to potential runtime mapping errors.

You can solve this problem for everything except enums by simply not using AutoMapper and writing the mapping code manually, but that doesn't work for enums as using a switch statement doesn't solve the problem, as there is no compile time check to ensure that all enum values are handled in the switch statement and no values missing from the destination enum.

## What?
Tofu is a source generator that generates mapping code for enums at compile time, ensuring that all enum values are handled and mapped correctly. If a new value is added to the source enum, the developer will get a compile time error if the corresponding value is not added to the destination enum as well.

Tofu is designed to be simple and easy to use, with minimal configuration required and to allow several options to customize the mapping behavior, such as:
- generate mapping based on enum names or enum values
- ignore certain enum values
- provide custom mapping for certain enum values
