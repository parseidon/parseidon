# Parseidon.SourceGen

A Roslyn source generator that automatically generates parsers from Parseidon grammar files (*.pgram) at compile time.

## Usage

1. Add the Parseidon.SourceGen package to your project:

```xml
<ItemGroup>
  <PackageReference Include="Parseidon.SourceGen" Version="1.0.0" />
</ItemGroup>
```

2. Add your grammar files (*.pgram) to your project and mark them as AdditionalFiles:

```xml
<ItemGroup>
  <AdditionalFiles Include="MyGrammar.pgram" />
</ItemGroup>
```

Or to include all .pgram files:

```xml
<ItemGroup>
  <AdditionalFiles Include="**/*.pgram" />
</ItemGroup>
```

3. The source generator will automatically find and parse all .pgram files during compilation and generate the corresponding parser classes.

## Example

Grammar file (`Calculator.pgram`):

```
@namespace MyApp.Parsers;
@class CalculatorParser;

Expression = Term (('+' / '-') Term)*;
Term = Factor (('*' / '/') Factor)*;
Factor = Number / '(' Expression ')';
$Number = [0-9]+ Spacing;
!Spacing = [ \t\r\n]*;
```

Usage in your code:

```csharp
using MyApp.Parsers;

var parser = new CalculatorParser();
var result = parser.Parse("2 + 3 * 4");

if (result.Successful)
{
    Console.WriteLine("Parse successful!");
    // Process the AST...
}
else
{
    foreach (var message in result.Messages)
    {
        Console.WriteLine($"{message.Type}: {message.Message}");
    }
}
```

## Features

- Automatically discovers *.pgram files at compile time
- Generates parser code directly into the compilation
- Full integration with the build process
- Proper error reporting for grammar parsing issues

## Technical Details

This source generator runs in a protected environment at compile time. All required dependencies (Parseidon.Parser, Parseidon.Helper, Humanizer) are included in the NuGet package to ensure they are available during code generation.
