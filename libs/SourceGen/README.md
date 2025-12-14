# Parseidon.SourceGen

Roslyn incremental source generator that turns `.pgram` files into parsers during compilation. Project home: https://github.com/parseidon/parseidon

## Install

Reference the package as an analyzer and mark `.pgram` files as `AdditionalFiles`:

```xml
<ItemGroup>
  <AdditionalFiles Include="Grammar/**/*.pgram" />
  <PackageReference Include="Parseidon.SourceGen" Version="<version>"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## What it does

- Scans all `.pgram` files supplied via `AdditionalFiles`.
- Uses the built-in `ParseidonParser` to validate the grammar.
- Emits parser code as `<fileName>.g.cs` with no runtime dependencies.
- Also produces TextMate grammar JSON and VS Code language configuration data when requested via `ParseidonVisitor` outputs.

## Diagnostics

- `PGRAM001` / `PGRAM011`: parsing errors or warnings in the grammar.
- `PGRAM002` / `PGRAM012`: code-generation errors or warnings.
- `PGRAM999`: unexpected generator exception.

## Tips

- Set `@namespace`, `@class`, and `@root` inside the grammar so emitted types match your project layout.
- Keep generator output under source control when reviewing changes, or regenerate with the CLI (`dotnet parseidon parser ...`) for debugging.
- The generator is marked as `DevelopmentDependency` to avoid being transitively referenced by consumers.
