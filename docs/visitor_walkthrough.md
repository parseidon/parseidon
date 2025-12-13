# Beginner guide: writing a visitor

This guide shows how to process the AST produced by your generated parser using a custom visitor. It assumes you already have a `.pgram` and generated parser (see `docs/grammar_walkthrough.md`).

## What the generated code provides

- A parser class (your `@class`) with a `Parse(string)` method that returns `ParseResult`.
- An `INodeVisitor` interface with one `Process<Rule>Name` method per grammar rule, plus `BeginVisit`/`EndVisit` hooks.
- `ParserMessage` objects that collect warnings/errors during parsing or visiting.

## Minimal flow

```csharp
var parser = new MyParser();
var result = parser.Parse("1+2*3");
if (!result.Successful) { /* handle parse errors in result.Messages */ }
var visitResult = result.Visit(new EvalVisitor());
// read visitResult.Messages for visitor diagnostics
```

## Step 1: define a context to hold state

Create a class to hold the data you want to build (e.g., a value, a list of tokens, a symbol table).

```csharp
class EvalContext
{
    public Stack<int> Values { get; } = new();
}
```

## Step 2: implement `INodeVisitor`

Implement the generated interface. For rules you do not care about, return `ProcessNodeResult.Success`.

```csharp
class EvalVisitor : INodeVisitor
{
    public object GetContext(ParseResult parseResult) => new EvalContext();

    public IVisitResult GetResult(object context, bool successful, IReadOnlyList<ParserMessage> messages)
    {
        var ctx = (EvalContext)context;
        return new SimpleVisitResult(successful, messages, ctx.Values.Peek());
    }

    public void BeginVisit(object context, ASTNode node) { }
    public void EndVisit(object context, ASTNode node) { }

    public ProcessNodeResult ProcessExprNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;
    public ProcessNodeResult ProcessTermNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessNumberNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var ctx = (EvalContext)context;
        if (int.TryParse(node.GetText(), out var value))
            ctx.Values.Push(value);
        else
            messages.Add(new ParserMessage($"Invalid number '{node.GetText()}'", ParserMessage.MessageType.Error, (0u, 0u)));
        return ProcessNodeResult.Success;
    }

    // ...implement other generated Process* methods or return Success if unused
}
```

Notes:

- The generated interface names follow your grammar rule names (e.g., `ProcessExprNode` for rule `Expr`).
- `node.GetText()` returns the matched source text; dropped or inlined rules do not appear as separate nodes.
- You can attach warnings/errors by adding `ParserMessage` entries.

## Step 3: wrap the visit result

Implement `IVisitResult` to expose what you computed.

```csharp
class SimpleVisitResult : IVisitResult
{
    public SimpleVisitResult(bool successful, IReadOnlyList<ParserMessage> messages, int value)
    {
        Successful = successful;
        Messages = messages;
        Value = value;
    }
    public bool Successful { get; }
    public IReadOnlyList<ParserMessage> Messages { get; }
    public int Value { get; }
}
```

## Step 4: run it and inspect messages

```csharp
var parser = new MyParser();
var parse = parser.Parse("1+2");
if (!parse.Successful)
{
    foreach (var m in parse.Messages) Console.WriteLine($"Parse {m.Type}: {m.Message}");
    return;
}
var visit = parse.Visit(new EvalVisitor());
foreach (var m in visit.Messages) Console.WriteLine($"Visit {m.Type}: {m.Message}");
Console.WriteLine($"Result = {(visit as SimpleVisitResult)!.Value}");
```

## Tips for beginners

- Start by handling only leaf nodes (numbers, identifiers); return `Success` for the rest. Add more rules gradually.
- Use `BeginVisit`/`EndVisit` if you need enter/exit actions (e.g., push/pop scopes).
- Keep your AST lean (drop punctuation in the grammar) so the visitor processes only meaningful nodes.
- Leverage `errorname` and `^` in the grammar to get clearer messages when parsing fails.
