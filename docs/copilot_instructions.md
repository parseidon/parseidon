# Parseidon Copilot Instructions

Note to Copilot: This file explains how to help developers use Parseidon in other projects. Examples stay ASCII-only.

## Quick Path

1. Create a new `.pgram` and set header (`@namespace`, `@class`, `@root`).
2. Define the `_` whitespace rule and reuse it.
3. Model tokens/rules with `-`, `:`, `$`, `^`; add `errorname` for clearer messages.
4. Generate the parser (CLI `dotnet parseidon parser` or Roslyn Source Generator) and emit the AST via `ast` if needed.
5. Implement a visitor using the generated interface; create context, implement `Process*`, return a result.

## Writing the Grammar (core rules)

- Minimal header:

```parseidon
@namespace: MyApp.Parsing;
@class: MyParser;
@root: Expr;
```

- Centralize whitespace via `_`:

```parseidon
!_         = (WhiteSpace | NewLine | Comment)*;
WhiteSpace = [ \t];
NewLine    = '\r\n' | [\r\n];
Comment    = '#' [^\r\n]*;
```

- AST control markers:
  - `-Rule`: drop node (separators, parentheses).
  - `:Rule`: inline, no separate node.
  - `$Rule`: keep matched text as a single node (identifiers, literals).
  - `^Rule`: use rule name for error reporting.
- Property `errorname` for friendlier messages:

```parseidon
^Expr = Term (AddOp Term)* {errorname: expression};
```

- Typical tokens and expression structure:

```parseidon
$Identifier = [a-zA-Z] [a-zA-Z0-9_]*;
$Number     = [0-9]+;
-LParen     = '(';
-RParen     = ')';
-AddOp      = '+' | '-';
-MulOp      = '*' | '/';
Expr        = Term (AddOp Term)*;
Term        = Factor (MulOp Factor)*;
Factor      = Number | :Group;
Group       = -LParen Expr -RParen;
```

- Avoid left recursion; prefer repetitions (`*`, `+`).
- Keep the AST lean: drop or inline syntax sugar.

## Generating the parser

- CLI (fast iteration):

```bash
dotnet parseidon parser path/to/grammar.pgram path/to/MyParser.cs -o override
```

- Inspect AST to verify drops/inlines:

```bash
dotnet parseidon ast path/to/grammar.pgram path/to/grammar.ast.yaml
```

- VS Code package/TextMate if needed:

```bash
dotnet parseidon vscode path/to/grammar.pgram out/vscode -o override
```

- Roslyn Source Generator (build integration):

```xml
<ItemGroup>
  <AdditionalFiles Include="Grammar/**/*.pgram" />
  <PackageReference Include="Parseidon.SourceGen" Version="<version>" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Building a visitor

- Generated parser returns `ParseResult` with `AST` and `Messages`.
- Generated `INodeVisitor` interface has `Process<Rule>Name` per grammar rule plus `BeginVisit`/`EndVisit`.
- Nodes for dropped (`-`) or inline (`:`) rules do not appear separately.
- Minimal flow:

```csharp
var parser = new MyParser();
var parse = parser.Parse("1+2*3");
if (!parse.Successful) return;
var visit = parse.Visit(new EvalVisitor());
```

- Example visitor skeleton:

```csharp
class EvalContext { public Stack<int> Values { get; } = new(); }

class EvalVisitor : INodeVisitor
{
    public object GetContext(ParseResult parseResult) => new EvalContext();

    public IVisitResult GetResult(object context, bool successful, IReadOnlyList<ParserMessage> messages)
    {
        var ctx = (EvalContext)context;
        return new SimpleVisitResult(successful, messages, ctx.Values.TryPeek(out var v) ? v : 0);
    }

    public void BeginVisit(object context, ASTNode node) { }
    public void EndVisit(object context, ASTNode node) { }

    public ProcessNodeResult ProcessExprNode(object context, ASTNode node, IList<ParserMessage> messages)
        => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessNumberNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var ctx = (EvalContext)context;
        if (int.TryParse(node.GetText(), out var value)) ctx.Values.Push(value);
        else messages.Add(new ParserMessage($"Invalid number '{node.GetText()}'", ParserMessage.MessageType.Error, (0u, 0u)));
        return ProcessNodeResult.Success;
    }

    // Implement further Process* methods as needed; otherwise return Success.
}

class SimpleVisitResult : IVisitResult
{
    public SimpleVisitResult(bool successful, IReadOnlyList<ParserMessage> messages, int value)
    { Successful = successful; Messages = messages; Value = value; }
    public bool Successful { get; }
    public IReadOnlyList<ParserMessage> Messages { get; }
    public int Value { get; }
}
```

## Copilot prompting hints

- "Create the `_` whitespace rule for Parseidon and place it between tokens." (expects the pattern above.)
- "Write a `.pgram` for simple expressions with `@namespace`, `@class`, `@root` and dropped parentheses." (AST should keep only meaningful nodes.)
- "Generate CLI commands to build the parser and AST from `foo.pgram` with override enabled."
- "Implement a visitor skeleton for rules `Expr`, `Term`, `Number`, returning a context object and collecting `ParserMessage` warnings."

## Troubleshooting

- Too many AST nodes: add `-` or `:` to rules that are just syntax sugar.
- Cryptic errors: use `^Rule` or `{errorname: friendly-name}`.
- Build cannot find parser: check `@namespace`/`@class`, `AdditionalFiles` paths, and that `Parseidon.SourceGen` is referenced as analyzer.
- CLI does not overwrite: pass `-o override` or use `backup`.
