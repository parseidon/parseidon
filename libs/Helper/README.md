# Parseidon.Helper

Utility helpers for parser and tooling development. Also used internally by Parseidon. Project home: https://github.com/parseidon/parseidon

## Install

```bash
dotnet add package Parseidon.Helper
```

## Included utilities

### ScopedStack<T>

- `EnterScope()` / `ExitScope()` start and end logical scopes; items pushed after `EnterScope` belong to that scope depth.
- `Push(T item)` stores an item at the current scope.
- `Pop()` removes and returns the most-recent item in the current or child scope; throws if none found.
- `Peek()` returns (without removing) the most-recent item in the current or child scope; throws if none found.
- `TryPeek()` returns the most-recent item or `null` if no item exists.
- `GetItemsInCurrentAndChildScopes()` enumerates items that are at or below the current scope depth.

### StringExtensions

- `ReplaceAt(string source, int index, int length, string replacement)` replaces a substring; throws if the range is invalid.
- `ReplaceAll((string Search, string Replace)[] rules)` applies multiple replacements in a single scan (first matching rule wins per position).
- `ContainsNewLine(string input)` detects any newline character (CR, LF, NEL, LS, PS).
- `Unescape(string value)` converts common escape sequences (\n, \r, \t, quotes, backslash, etc.).
- `FormatLiteral(string value, bool useQuotes)` returns a C#-style escaped literal; wraps in quotes when `useQuotes` is true.
- `TrimLineEndWhitespace(string input)` removes trailing spaces or tabs at line ends (multiline-aware).

## Usage

```csharp
using Parseidon.Helper;

var stack = new ScopedStack<string>();
stack.EnterScope();
stack.Push("root");
stack.EnterScope();
stack.Push("child");
string current = stack.Peek(); // "child"
stack.ExitScope();

string literal = "Hello\nworld".FormatLiteral(useQuotes: true);
// literal => "\"Hello\\nworld\""
```

## Related packages

- `Parseidon.Parser` consumes these helpers when compiling grammars.
- `Parseidon.SourceGen` ships them alongside the analyzer so generator code runs without extra references.
