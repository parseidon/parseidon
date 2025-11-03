# Parseidon Source Generator Example

This example project demonstrates how to use the Parseidon.SourceGen source generator to automatically generate parsers from grammar files at compile time.

## Overview

This project includes:
- **Calculator.pgram**: A simple calculator grammar that defines arithmetic expressions
- **Program.cs**: Example code that uses the generated parser
- The `CalculatorParser` class is automatically generated during compilation

## How It Works

1. The `.pgram` file is marked as an `AdditionalFile` in the project file
2. During compilation, the Parseidon.SourceGen source generator finds the `.pgram` file
3. The generator parses the grammar and generates a complete parser class
4. The generated parser is available to use in your code

## Running the Example

```bash
cd apps/SourceGenExample
dotnet run
```

## Grammar File (Calculator.pgram)

The example uses a simple calculator grammar that can parse arithmetic expressions:

```
@namespace Parseidon.SourceGenExample.Generated;
@class CalculatorParser;

Grammar     = Spacing Expression;
Expression  = Term (('+' / '-') Term)*;
Term        = Factor (('*' / '/') Factor)*;
Factor      = Number / '(' Expression ')';
$Number     = [0-9]+ Spacing;
!Spacing    = [ \t\r\n]*;
```

This grammar can parse expressions like:
- `2 + 3`
- `5 * 6`
- `2 + 3 * 4`
- `(2 + 3) * 4`

## Key Configuration

In the `.csproj` file:

```xml
<ItemGroup>
  <!-- Reference the source generator -->
  <ProjectReference Include="..\..\libs\SourceGen\Parseidon.SourceGen.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>

<ItemGroup>
  <!-- Include grammar files for source generation -->
  <AdditionalFiles Include="*.pgram" />
</ItemGroup>
```

## Using the Generated Parser

```csharp
using Parseidon.SourceGenExample.Generated;

// Create parser instance
var parser = new CalculatorParser();

// Parse input
var result = parser.Parse("2 + 3 * 4");

if (result.Successful)
{
    Console.WriteLine("Parse successful!");
    // Access the AST through result.RootNode
}
else
{
    foreach (var message in result.Messages)
    {
        Console.WriteLine($"{message.Type}: {message.Message}");
    }
}
```
