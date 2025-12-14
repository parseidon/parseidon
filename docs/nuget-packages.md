# NuGet packages

Overview of the packages published for Parseidon and how to consume them. Project home: https://github.com/parseidon/parseidon

| Package             | Purpose                                                                                                   | Install                                                                                             |
| ------------------- | --------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| dotnet-parseidon    | CLI tool for generating parser code, TextMate grammars, VS Code packages, or AST dumps.                   | `dotnet tool install -g dotnet-parseidon`                                                           |
| Parseidon.SourceGen | Roslyn incremental generator that emits parsers during compilation.                                       | Add `Parseidon.SourceGen` as an analyzer reference and include `.pgram` files as `AdditionalFiles`. |
| Parseidon.Helper    | Utility helpers used by the parser and generator (scoped stack and string helpers).                       | `dotnet add package Parseidon.Helper`                                                               |
| Parseidon.Parser    | Core grammar compiler that turns `.pgram` files into C# parsers, TextMate grammars, and VS Code packages. | `dotnet add package Parseidon.Parser`                                                               |

## Source generator quickstart

```xml
<ItemGroup>
  <AdditionalFiles Include="Grammar/**/*.pgram" />
  <PackageReference Include="Parseidon.SourceGen" Version="<version>"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## CLI quickstart

```bash
# Parser code
dotnet parseidon parser hello.pgram out/DemoParser.cs

# VS Code package
dotnet parseidon vscode hello.pgram out/vscode -o override
```

## Further reading

- [docs/cli.md](docs/cli.md) for all CLI commands and options.
- [docs/sourcegen.md](docs/sourcegen.md) for generator behavior and diagnostics.
- [docs/grammar.md](docs/grammar.md) and [docs/grammar_walkthrough.md](docs/grammar_walkthrough.md) for authoring grammars.
- [docs/textmate_vscode.md](docs/textmate_vscode.md) for TextMate/VS Code emission details.
