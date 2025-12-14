# dotnet-parseidon

Global dotnet tool that compiles `.pgram` grammar files into C# parsers, TextMate grammars, or full VS Code language packages. Project home: https://github.com/parseidon/parseidon

## Install

```bash
dotnet tool install -g dotnet-parseidon
```

## Commands

- `parser`: generate a C# parser file.
- `vscode`: emit a VS Code package (`package.json`, `language-configuration.json`, `syntaxes/*.tmLanguage.json`).
- `textmate`: write a standalone TextMate grammar JSON.
- `ast`: dump the parsed AST as YAML for inspection.

Common option: `--override|-o {ask|abort|backup|override}` controls how existing outputs are handled.

## Examples

```bash
# Create a parser and keep any existing file as .bak
dotnet parseidon parser hello.pgram DemoParser.cs -o backup

# Generate a VS Code package with a specific version and override file
dotnet parseidon vscode hello.pgram out/vscode -o override -v 1.0.0 --package-json-override ./package.patch.json

# Export a TextMate grammar only
dotnet parseidon textmate hello.pgram syntaxes/demo.tmLanguage.json
```

See the full CLI reference at https://github.com/parseidon/parseidon/blob/main/docs/cli.md
