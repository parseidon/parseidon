# Parseidon.Parser

Core library that parses `.pgram` grammar files and turns them into C# parsers, TextMate grammars, and VS Code language packages. Used by the Parseidon CLI and the `Parseidon.SourceGen` analyzer. Project home: https://github.com/parseidon/parseidon

## Install

```bash
dotnet add package Parseidon.Parser
```

## When to use

- Build custom tooling that consumes `.pgram` grammars without invoking the CLI.
- Generate parser code at design time or inside bespoke build steps.
- Inspect or transform the grammar Abstract Syntax Tree (AST) directly.

## Quickstart

```csharp
using Parseidon.Parser;

string grammar = File.ReadAllText("hello.pgram");
var parser = new ParseidonParser();
var parseResult = parser.Parse(grammar);
if (!parseResult.Successful)
{
    foreach (var message in parseResult.Messages)
    {
        Console.Error.WriteLine($"{message.Type}: ({message.Row},{message.Column}) {message.Message}");
    }
    return;
}

var visitor = new ParseidonVisitor();
var visitResult = parseResult.Visit(visitor);
if (visitResult is ParseidonVisitor.IGetResults outputs && visitResult.Successful)
{
    var parserCode = outputs.GetParserCode();
    File.WriteAllText("DemoParser.g.cs", parserCode.Output);

    var tmLanguage = outputs.GetTextMateGrammar();
    var vscode = outputs.GetVSCodePackage("1.0.0", File.ReadAllText, packageJsonOverridePath: null);
    // tmLanguage.Output and vscode.Output contain serialized JSON content
}
```

Each `CreateOutputResult` (`parserCode`, `tmLanguage`, `vscode`, `GetLanguageConfig()`) carries `Successful`, `Output`, and `Messages` so you can surface warnings and errors in your own pipeline.
