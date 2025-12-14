# Using `copilot_instructions.md`

Audience: humans configuring or prompting Copilot. Purpose: keep prompts concise and consistent when generating Parseidon grammars, parsers, and visitors.

## How to use it
- Read or share the file [docs/copilot_instructions.md](docs/copilot_instructions.md) before asking Copilot for Parseidon help.
- When prompting Copilot, copy only the relevant sections (grammar basics, parser generation, visitor skeleton). Avoid pasting the whole file unless needed.
- Keep prompts short and specific (goal, inputs, outputs). Example: "Create a `.pgram` with @namespace MyApp.Parsing, @class MyParser, @root Expr; include `_` whitespace, drop parens, add errorname on Expr."
- If Copilot drifts, remind it to follow the markers (`-`, `:`, `$`, `^`, `errorname`) and lean AST guidance from the file.

## Recommended prompt patterns
- Grammar: "Using the rules in docs/copilot_instructions.md, draft a `.pgram` for simple arithmetic: headers set, `_` whitespace, drop/inline punctuation, keep identifiers/numbers with `$`."
- Parser via CLI: "Give the CLI commands (with -o override) to generate parser and AST from `foo.pgram` as described in docs/copilot_instructions.md."
- Visitor: "Create a visitor skeleton per docs/copilot_instructions.md for rules Expr, Term, Number; include context object and message collection."

## When to include the file contents
- Small tasks: quote only the relevant subsection to keep prompt size down.
- Larger/ambiguous tasks: include the full file to pin Copilot to the correct marker semantics and CLI/source-gen usage.
- Do not embed unrelated project files alongside it; keep the context focused.

## Tips for better results
- Mention that examples are ASCII-only.
- State the target @namespace/@class/@root up front.
- Ask Copilot to keep the AST lean (drop/inline syntax sugar) and avoid left recursion.
- For troubleshooting, point Copilot to the "Troubleshooting" section in [docs/copilot_instructions.md](docs/copilot_instructions.md).
