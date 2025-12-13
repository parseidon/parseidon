# TextMate and VS Code generation

Parseidon produces TextMate grammars and full VS Code language packs directly from `.pgram` files. It reuses the grammar structure to build the regular expressions needed for syntax highlighting.

## Authoring TextMate rules in `.pgram`

- TextMate rules use identifiers prefixed with `!`, e.g. `!Root = <Expr>;` where `Expr` is a grammar rule wrapped in a TextMate pattern.
- There are two kinds of how TextMate rules can be defined:
  1. Defining a single match pattern
  2. Defining a begin- and end-pattern and included rules that can occur in any order between them.
- Match forms:
  - `<Rule>` wraps a grammar rule; an optional scope name may prefix the match (e.g. `source.demo.expr:<Expr>`).
  - Grouped sequences: `( <Match> <Match> )`.
  - Regex terminals for TextMate: `!'...';` for inline regex patterns.
  - Includes: `[!Other, Identifier]` mixes other TextMate rules (`!Other`) and grammar rules (`Identifier`).
- The TextMate root definition must correspond to `@root` (write it with `!Root`).
- TextMate sequences can carry scopes.
- TextMate repository entries are built from both explicit `!` rules and any grammar rule with a `tmpattern` property (auto-injected).

### Single match definitions

A single match definition defines a regular expression, which is used to match it agains the input. The matched text is tagged with the given scope. Also submatches can be defined to tag parts of the match with different scopes.

TextMate-Definition (simplified):

```
# Unscoped definition
!Identifier = MatchPattern;

# Scoped definition
!Identifier = scope.name:(MatchPattern);
```

- `MatchPattern` can be:
  - A grammar definition wrapped in `< >` (The definition can be given a scope name with `scope.name:< >`).
  - A grouped sequence `( ... )`.
  - A regex match `!'...';`.

```parseidon
# Matches the regular expression
# Generated regular expression => (?:\b(if|else|return)\b)
!Keyword = keyword.control:(!'\b(if|else|return)\b');

# Matches the parseidon grammar expression
# Generated regular expression => (?:("(?:([a-zA-Z0-9])*)"))
!QuotedString = string.quoted:<( Quote ([a-zA-Z0-9])* Quote )>;
:Quote = '"';

# Matches a grammar rule with a scope
# Generated regular expression => (?:([a-zA-Z][a-zA-Z0-9_]*))
!Identifier = variable.name:<Identifier>;
Identifier = [a-zA-Z] [a-zA-Z0-9_]*;

# Matches a sequence with submatches
# Generated regular expression => (?:([a-zA-Z][a-zA-Z0-9_]*)(?:=)([0-9]+)(?:;))
!Assignment =
  meta.assignment:(
    variable.name:<Identifier> !'=' constant.numeric:<Number> !';'
  );
Number = [0-9]+;
```

### Begin-End definitions

A begin-end definition defines a begin pattern, an end pattern, and a list of included rules that can appear in any order between them. The full match can be tagged with a scope. Also submatches in the begin and end patterns can be tagged.

TextMate-Definition (simplified):

```
# Unscoped definition
!Identifier = BeginPattern IncludeList EndPattern;

# Scoped definition
!Identifier = scope.name:(BeginPattern IncludeList EndPattern);
```

- `BeginPattern` and `EndPattern` are identical to `MatchPattern` from single match definitions.
- `IncludeList` is a list of includes in square brackets: `[!Other, Identifier]`.
  - It can reference other TextMate definitions (`!Other`) and grammar definitions (`Identifier`).
    Grammar definitions must have a property `tmpattern`.

```parseidon
# Matches a block with begin and end patterns and included rules
!ValueList =
  meta.valuelist:(
    <Identifier> !'=' [!Boolean, Number] !';'
  );
Identifier = [a-zA-Z] [a-zA-Z0-9_]*;
!Boolean = constant.boolean:<'true' | 'false'>;
Number = [0-9]+ {tmpattern: constant.numeric};
```

## Scope names

Scope names follow the TextMate convention of dot-separated parts indicating the type of token. Common prefixes:

- `source.<language>`: top-level scope for the language.
- `meta.<construct>`: a language construct (e.g. `meta.function`).
- `keyword.<type>`: keywords (e.g. `keyword.control`).
- `entity.<type>`: entities (e.g. `entity.name.function`).
- `storage.<type>`: storage keywords (e.g. `storage.type`).
- `variable.<type>`: variables (e.g. `variable.name`).
- `string.<type>`: strings (e.g. `string.quoted`).
- `constant.<type>`: constants (e.g. `constant.numeric`).
- `support.<type>`: language support constructs (e.g. `support.function`).

For more info, see the [TextMate scope naming conventions](https://macromates.com/manual/en/language_grammars#naming_conventions).

## Options that influence TextMate output

- `@displayname`: Human-friendly language name used in grammar metadata.
- `@scopename`: Base TextMate scope (e.g. `source.demo`), used for the root include.
- `@filetype`: Comma/semicolon-separated extensions without dots (e.g. `demo,dmo`).
- `@linecomment` (optional): Line comment prefix used for language configuration.

## Properties for TextMate generation

- `tmscope`: Override scope for a rule in TextMate output.
- `tmpattern`: Turn this grammar rule into a TextMate repository entry with the given scope; helpful for terminals/tokens.

## VS Code–only specific

### Header options

- `@version`: Version written to `package.json`; normalized to a Marketplace-compliant dotted numeric form.
- `@name` (optional): Explicit VS Code language id; defaults to a lowercase, spaceless `@displayname`.
- `@packagejsonmerge`: Path (can be relative to the grammar file) to JSON merged over the generated `package.json`.

### Definition properties

- `quote`: Mark as a quote pair for language configuration auto-closing.
- `bracketopen` / `bracketclose`: Pair rules to build bracket/auto-closing/surrounding entries.

### CLI options

- CLI overrides:
  - `--version|-v`: Overrides the version in `package.json`.
  - `--package-json-override|-p`: JSON merged over `package.json` (wins on conflicts); paths resolve relative to the grammar file when not absolute.
- Output layout from `dotnet-parseidon vscode`: `package.json`, `language-configuration.json`, `syntaxes/<language>.tmLanguage.json` written to the target folder.
