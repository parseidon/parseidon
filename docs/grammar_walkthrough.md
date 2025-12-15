# Step-by-step: write your first `.pgram` grammar

A beginner-friendly path to define a grammar that produces a clear, reusable AST. This guide focuses only on the parser grammar (no TextMate details).

## 1) Add the minimal header

At the top of your `.pgram`, set the required options so the generated parser has a namespace, class, and entry point:

```parseidon
@namespace: MyApp.Parsing;
@class: MyParser;
@root: Expr;
```

- `@namespace`: C# namespace in the generated code.
- `@class`: Parser class name.
- `@root`: The start rule; the parser begins here.

Keep the header short; you can add more later.

## 2) Define whitespace once (and forget about it)

Parsing usually needs to skip spaces, newlines, and comments. Define a reusable spacing rule named `_` and use it between tokens so you do not sprinkle whitespace everywhere.

```parseidon
!_         = (WhiteSpace | NewLine | Comment)*;
WhiteSpace = [ \t];
NewLine    = '\r\n' | [\r\n];
Comment    = '#' [^\r\n]*;
```

Use `_` inline to keep rules readable:

```parseidon
Assignment = Identifier_-Equal_Expression_-Semicol_;
```

This expands to `Identifier` + spacing + `=` + spacing + `Expression` + spacing + `;` + spacing.

## 3) Create simple tokens (terminals)

Start with literals and character classes. Use markers to control the AST:

- `$Rule`: keep the entire matched text as one AST node (useful for numbers, identifiers).
- `-Rule`: drop this node from the AST (punctuation, separators).
- `:Rule`: inline the referenced rule so it does not create its own node.
- `^Rule`: report this rule’s name on errors, even when failures occur deeper inside.

Examples:

```parseidon
# Keep identifiers and numbers as single nodes
$Identifier = [a-zA-Z] [a-zA-Z0-9_]*;
$Number     = [0-9]+;

# Drop punctuation so it does not clutter the AST
-Equal   = '=';
-Semicol = ';';

# Inline quotes to avoid extra nodes in strings
StringLiteral = -Quote [^"]* -Quote;
:Quote        = '"';
```

Tip: use a negated class for the string body, e.g. `[^"]*`.

## 4) Build expressions with sequences and choices

Compose non-terminals from your tokens. Use parentheses to group and `|` for alternatives. Apply quantifiers `?`, `*`, `+` to primaries.

```parseidon
Expr   = Term (AddOp Term)*;
Term   = Factor (MulOp Factor)*;
Factor = Number | Group;
Group  = -LParen Expr -RParen;

-LParen = '(';
-RParen = ')';
-AddOp  = '+' | '-';
-MulOp  = '*' | '/';
```

- `*` allows zero or more repetitions; here it makes `Expr` parse chains like `a + b - c`.
- Dropping parens keeps the AST focused on values and operators.

Need to block certain prefixes? Use the negation operator `^` in front of any expression (literal, group, alternation, or rule reference). The operand is tested at the current position; if it matches, parsing fails. If it does **not** match, `^` still consumes one character so parsing can continue.

```parseidon
# Disallow identifiers that start with a digit
Identifier = ^Digit IdentifierRest;
Digit      = [0-9];
IdentifierRest = [a-zA-Z0-9_]*;

# Skip one character unless a keyword is ahead
Value = ^('not' | 'none') IdentifierRest;
```

## 5) Aim for a clean, reusable AST

Good ASTs are shallow where possible and keep only meaningful nodes:

- Drop syntax sugar (parentheses, commas, keywords) unless you need them later.
- Use `$` on tokens that are reused elsewhere (identifiers, literals) so downstream code can read their text directly.
- Inline (`:`) helper rules that only group characters (quotes, minor wrappers) to avoid noisy nodes.
- Use `^` on user-facing constructs so error messages name the rule users recognize (e.g., `^Expr`).

Example with cleaner AST intent:

```parseidon
^Expr   = Term (AddOp Term)*;
Term    = Factor (MulOp Factor)*;
Factor  = Number | Group;
Group   = -LParen Expr -RParen;

$Identifier = [a-zA-Z] [a-zA-Z0-9_]*;
$Number     = [0-9]+;
-LParen     = '(';
-RParen     = ')';
-AddOp      = '+' | '-';
-MulOp      = '*' | '/';
```

The resulting AST focuses on `Expr`, `Term`, `Factor`, `Group`, `Number`, `Identifier`—everything else is dropped or inlined.

## 6) Add friendly error labels when needed

If a rule represents a concept the user knows, add `errorname` to give clearer messages. Attach properties after the expression:

```parseidon
^Expr = Term (AddOp Term)* {errorname: expression};
```

Now errors inside `Expr` report "expression" instead of the raw rule name.

## 7) Sanity-check your grammar

- Ensure the `@root` rule exists and can reach all needed constructs.
- Avoid left recursion; use repetition (`*`, `+`) instead of leading self-references.
- Keep whitespace handling centralized via `_` so every rule tolerates spacing.
- Prefer dropping punctuation and inlining trivial wrappers to keep the AST small and clear.

You now have a self-contained `.pgram` ready to generate a parser and a clean AST for further processing. To inspect and test your grammar quickly, render the AST with the CLI:

```bash
dotnet parseidon ast mygrammar.pgram mygrammar.ast.yaml
```

Open the YAML output to see the exact node structure—this is the fastest way to verify whether drops, inlines, and `$` markers give you the AST shape you want.
