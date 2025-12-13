# CLI reference

Parseidon ships as a dotnet tool `dotnet-parseidon`. All commands read a `.pgram` grammar and emit artifacts without additional runtime dependencies.

## Common options

- `--override|-o {ask|abort|backup|override}`: what to do if the destination exists (default `ask`). `backup` keeps the old file by appending `.bak`, `abort` stops, `ask` prompts, `override` overwrites.

## Commands

### `parser`

Create a C# parser class.

```bash
dotnet parseidon parser GRAMMAR-FILE OUTPUT-FILE [-o override]
```

- Output: C# source file with parser, visitor interfaces, and support types. A banner at the top indicates generation time.
- Typical use: `dotnet parseidon parser sample.pgram SampleParser.cs`.

### `vscode`

Generate a complete VS Code language package (TextMate grammar, language configuration, package.json).

```bash
dotnet parseidon vscode GRAMMAR-FILE OUTPUT-FOLDER [-o override] [--version 1.2.3] [--package-json-override path/to/override.json]
```

- Writes to the folder (created/overwritten according to `--override`):
  - `syntaxes/<name>.tmLanguage.json`
  - `language-configuration.json`
  - `package.json`
- `--version|-v` overrides the version placed in `package.json`. The value is normalized to a Marketplace-compliant format (up to four numeric parts).
- `--package-json-override|-p` merges the supplied JSON file over the generated `package.json` (keys in the override win). Relative paths are resolved from the grammar file location.

### `textmate`

Generate a TextMate grammar JSON file.

```bash
dotnet parseidon textmate GRAMMAR-FILE OUTPUT-FILE
```

- Output is suitable for editors that consume `.tmLanguage.json`.

### `ast`

Emit the parsed Abstract Syntax Tree as YAML for inspection/debugging.

```bash
dotnet parseidon ast GRAMMAR-FILE OUTPUT-FILE
```

## Examples

```bash
# Keep an existing parser as .bak
dotnet parseidon parser hello.pgram DemoParser.cs -o backup

# Generate a TextMate grammar
dotnet parseidon textmate hello.pgram syntaxes/demo.tmLanguage.json

# Build a VS Code package with a specific version and override content
dotnet parseidon vscode hello.pgram out/vscode -o override -v 1.0.0 --package-json-override ./package.patch.json
```
