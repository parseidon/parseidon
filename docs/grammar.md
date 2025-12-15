# Grammar guide (`.pgram`)

A `.pgram` file contains global options followed by grammar definitions and optional TextMate definitions. Every definition ends with `;`. Comments start with `#`.

## Grammar definitions

Definition (simplified):

```
Identifier = Expression;
```

An `Expression` supports:

- Sequences and alternatives: `A B | C`.
- Parentheses group expressions: `(A B | C)`.
- Quantifiers: `?` (optional), `*` (zero or more), `+` (one or more).
- Negation: `^<expression>` succeeds only if the expression does **not** match at the current position; it still consumes one character.
- Definition references, string literals (`'if'`), regex character classes (`[a-z]`, `[0-9]+`), or any character (`.`).
- Regex literals use character classes; escapes follow standard regex escaping in character classes.

### Negation (`^`)

Use `^` in front of any expression (literal, group, alternation, or definition reference). The operand is tried at the current position; if it matches, parsing fails. If it does **not** match, one character is consumed and parsing continues. This is different from the definition-level `^Rule` marker that only changes error reporting.

```parseidon
# Forbid identifiers starting with digits
Identifier = ^Digit [a-zA-Z0-9_]*;
Digit      = [0-9];

# Block specific keywords as a prefix
Value = ^('not' | 'none') IdentifierRest;

# Disallow a referenced rule at the current position
Text = ^Forbidden AnyRest;
Forbidden = 'DROP';
AnyRest = .+;
```

The parsed character is included as a child node; combine it with `-` if you do not need it in the AST.

### Examples

```parseidon
# Match a keyword
Keyword = 'if' | 'else' | 'while';

# Match an identifier (letter followed by letters/digits/underscores)
Identifier = [a-zA-Z] [a-zA-Z0-9_]*;

# Match a simple expression
Expr = Term (('+' | '-') Term)*;
Term = Factor (('*' | '/') Factor)*;
Factor = Number | '(' Expr ')';
Number = [0-9]+;
```

## Control the parser behaviour

Definitions can be annotated with special symbols to modify their behavior.

### `:` (`TreatInline`): Inline the referenced definition into its parent (no separate AST node).

Includes the referenced definition directly into the parent expression, avoiding the creation of a separate AST node for it.

```parseidon
# Creates code like
# QuotedString = ParseFor('"' ([a-zA-Z0-9])* '"')
# instead of
# QuotedString = ParseFor(Quote) -> ParseFor(([a-zA-Z0-9])*) -> ParseFor(Quote)
# Quote = ParseFor('"')

QuotedString = Quote ([a-zA-Z0-9])* Quote;
:Quote = '"';
```

The `TreatInline`-suffix can also be used in an `Expression`.

```parseidon
QuotedString = :Quote ([a-zA-Z0-9])* :Quote;
Quote = '"';
```

### `-` (`Drop`): Do not emit this node into the AST.

Exclude the matched source from the AST, since it is not relevant for further processing.

```parseidon
# Drop quotes from a string literal, since they are not needed in the AST

QuotedString = Quote ([a-zA-Z0-9])* Quote;
-Quote = '"';
```

The `Drop`-suffix can also be used in an `Expression`.

```parseidon
# Drop quotes from a string literal, since they are not needed in the AST

QuotedString = -Quote ([a-zA-Z0-9])* -Quote;
Quote = '"';
```

### `$` (`IsTerminal`): Mark as terminal; `$$` prevents escaping the output when emitting code.

Treats the matched text (including the sub-expressions) as a terminal value in the AST. This is useful for tokens like identifiers or literals.

```parseidon
# In the AST there will be one node 'Value' with the full matched text whether it's a quoted string or a number

$Value = QuotedString | Number;
Number = [0-9]+;
QuotedString = -'"' ([a-zA-Z0-9])* -'"';
```

### `^` (`UseDefinitionNameAsError`): Use name of this Definition is used for error reporting.

When a parsing error occurs within this definition or a subdefinition, the parser will report the name of this definition in the error message, making it easier to identify where the error happened.

```parseidon
# If parsing fails within a subdefinition of Expr, the error will always reference 'Expr' instead of the specific subdefinition.

^Expr = Term (('+' | '-') Term)*;
Term = Factor (('*' | '/') Factor)*;
Factor = Number | '(' Expr ')';
Number = [0-9]+;
```

### Properties on definitions

Add after the expression in braces `{}`. Keys are identifiers; values can be string literals, numbers, booleans, or identifiers. Commas separate pairs; a lone key is treated as boolean true.
Common properties:

For parser generation there is only one used property:

- `errorname`: Defines a more user-friendly name for error messages than the definition name.

## Header options (`@key: value;`)

With header options, global settings for the parser can be defined. They appear at the top of the grammar file before any definitions. Common options:

- `@namespace`: C# namespace for the generated parser.
- `@class`: C# class name for the generated parser.
- `@root`: Root definition; This is the first definition that is used to pase the input.
- `@nointerface` (optional): Skip generating the visitor interfaces.

### Comments and whitespace

Comments start with `#`.

# Special whitespace definition

Usulally an identifier is a given name for a definition. ([a-zA-Z][a-zA-Z0-9]\*).
Since your grammar must also parse the whitespace and linebreaks beween token, you have to define a definition for that like this:

```parseidon
!Spacing   = (WhiteSpace | NewLine | Comment)*;
WhiteSpace = [ \t];
NewLine    = '\r\n' | [\r\n];
Comment    = '#' [^\r\n]*;
```

That makes the grammar difficult to read, because you have to add `!Spacing*` between every token.

To simplify this, Parseidon allows a definition `_`. With this it looks like this:

```parseidon
!_         = (WhiteSpace | NewLine | Comment)*;
WhiteSpace = [ \t];
NewLine    = '\r\n' | [\r\n];
Comment    = '#' [^\r\n]*;
```

```parseidon
# Using the _-definition could make this
Definition = Identifier Spacing -Equal Spacing Expression Spacing LineEnd Spacing;

# into this
Definition = Identifier_-Equal_Expression_LineEnd_;
```
