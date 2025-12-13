# Source generator usage

The `Parseidon.SourceGen` package provides a Roslyn incremental source generator that turns `.pgram` files into C# parsers during compilation.

## Referencing the generator

Add the generator as an analyzer reference and pass `.pgram` files via `AdditionalFiles`:

```xml
<ItemGroup>
  <AdditionalFiles Include="Grammar/**/*.pgram" />
  <PackageReference Include="Parseidon.SourceGen" Version="<version>" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## How it works

1. The generator scans all `AdditionalFiles` ending in `.pgram`.
2. Each file is parsed with the built-in `Parseidon.Parser.ParseidonParser`.
3. If parsing succeeds, the grammar is visited and code is produced (same as the CLI `parser` command).
4. Generated output is added as `<fileName>.g.cs` to the compilation.

## Diagnostics

Diagnostics are reported at the grammar source location:

- `PGRAM001`: Grammar parsing error.
- `PGRAM011`: Grammar parsing warning.
- `PGRAM002`: Code generation error.
- `PGRAM012`: Code generation warning.
- `PGRAM999`: Unexpected exception inside the generator.

## Tips

- Keep `@namespace`, `@class`, and `@root` set in the grammar; they shape the emitted types.
- Warnings about unknown options/properties can signal typos before code is emitted.
- Generated parsers are dependency-free; include the emitted `.g.cs` in your project output as usual.
