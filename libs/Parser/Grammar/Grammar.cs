using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Humanizer;
using Parseidon.Helper;
using Parseidon.Parser.Grammar.Terminals;
using Parseidon.Parser.Grammar.Blocks;
using Parseidon.Parser.Grammar.Operators;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Collections.Immutable;

namespace Parseidon.Parser.Grammar;

public class Grammar : AbstractNamedElement
{
    public Grammar(List<Definition> definitions, List<TMDefinition> tmDefinitions, List<ValuePair> options, MessageContext messageContext, ASTNode node) : base("", messageContext, node)
    {
        Definitions = definitions;
        TMDefinitions = tmDefinitions;
        Options = options;
        CheckDuplicatedDefinitions(Definitions);
        Definitions.ForEach((element) => element.Parent = this);
        CheckTreatInlineCycles();
    }

    public List<Definition> Definitions { get; }
    public List<TMDefinition> TMDefinitions { get; }
    public List<ValuePair> Options { get; }

    internal const String GrammarOptionNamespace = "namespace";
    internal const String GrammarOptionClass = "class";
    internal const String GrammarOptionRoot = "root";
    internal const String GrammarOptionNoInterface = "nointerface";
    internal const String GrammarPropertyQuote = "quote";
    internal const String GrammarPropertyBracketOpen = "bracketopen";
    internal const String GrammarPropertyBracketClose = "bracketclose";
    internal const String GrammarPropertyErrorName = "errorname";
    internal const String TextMateOptionDisplayName = "displayname";
    internal const String TextMateOptionScopeName = "scopename";
    internal const String TextMateOptionFileType = "filetype";
    internal const String TextMateOptionLanguageName = "name";
    internal const String TextMateOptionVersion = "version";
    internal const String TextMateOptionLineComment = "linecomment";
    public const String VSCodeOptionPackageJsonMerge = "packagejsonmerge";
    internal const String TextMatePropertyScope = "tmscope";
    internal const String TextMatePropertyPattern = "tmpattern";

    public CreateOutputResult ToTextMateGrammar(MessageContext messageContext)
    {
        List<ParserMessage> messages = new List<ParserMessage>();
        TextMateGrammarDocument document = new TextMateGrammarDocument();
        Boolean successful = false;
        AddUnknownIdentifierWarnings(messages);
        try
        {
            TMDefinition rootDefinition = GetTMRootDefinition();

            document = new TextMateGrammarDocument
            {
                DisplayName = GetOptionValue(Grammar.TextMateOptionDisplayName),
                ScopeName = GetOptionValue(Grammar.TextMateOptionScopeName),
                FileTypes = GetFileTypes(),
                Patterns = new List<TMDefinition.TextMatePatternInclude>() { new TMDefinition.TextMatePatternInclude() { Include = $"#{rootDefinition.Name.ToLower()}" } },
                Repository = GetTextMateRepository(this, messages)
            };
            successful = true;
        }
        catch (GrammarException e)
        {
            messages.Add(new ParserMessage(e.Message, ParserMessage.MessageType.Error, (e.Row, e.Column)));
        }
        JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new KeyValuePairArrayConverter() }
        };
        return new CreateOutputResult(successful, JsonSerializer.Serialize(document, serializerOptions), messages);
    }

    public static String MergePackageJson(String generatedPackageJson, String overrideJsonContent)
    {
        JsonNode baseNode = JsonNode.Parse(generatedPackageJson) ?? new JsonObject();
        JsonNode overrideNode = JsonNode.Parse(overrideJsonContent) ?? new JsonObject();
        JsonNode merged = MergeJsonNodes(baseNode, overrideNode);
        return merged.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static JsonNode MergeJsonNodes(JsonNode baseNode, JsonNode overrideNode)
    {
        if (baseNode is JsonObject baseObj && overrideNode is JsonObject overrideObj)
        {
            foreach (var kvp in overrideObj)
            {
                if (kvp.Value is JsonObject overrideChild && baseObj[kvp.Key] is JsonObject baseChild)
                    baseObj[kvp.Key] = MergeJsonNodes(baseChild, overrideChild);
                else
                    baseObj[kvp.Key] = kvp.Value?.DeepClone();
            }
            return baseObj;
        }

        return overrideNode.DeepClone();
    }

    private static String NormalizeMarketplaceVersion(String rawVersion, List<ParserMessage> messages)
    {
        if (String.IsNullOrWhiteSpace(rawVersion))
        {
            messages.Add(new ParserMessage("The version is missing and must contain up to four numeric parts.", ParserMessage.MessageType.Error, (0u, 0u)));
            return rawVersion;
        }

        String versionWithoutMetadata = rawVersion.Split('+')[0];
        String[] mainAndPrerelease = versionWithoutMetadata.Split(new[] { '-' }, 2, StringSplitOptions.RemoveEmptyEntries);
        String mainPart = mainAndPrerelease.Length > 0 ? mainAndPrerelease[0].Trim() : String.Empty;
        String? prereleasePart = mainAndPrerelease.Length > 1 ? mainAndPrerelease[1].Trim() : null;

        String[] mainSegments = mainPart.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (mainSegments.Length == 0 || mainSegments.Length > 4)
        {
            messages.Add(new ParserMessage($"The version '{rawVersion}' is not valid. Use one to four numeric parts separated by '.'.", ParserMessage.MessageType.Error, (0u, 0u)));
            return rawVersion;
        }

        List<Int32> versionNumbers = new List<Int32>();
        foreach (String segment in mainSegments)
        {
            if (!Int32.TryParse(segment, out Int32 numericValue) || numericValue < 0)
            {
                messages.Add(new ParserMessage($"The version '{rawVersion}' is not valid. Each part must be a non-negative number less than 2147483648.", ParserMessage.MessageType.Error, (0u, 0u)));
                return rawVersion;
            }
            versionNumbers.Add(numericValue);
        }

        if (versionNumbers.Count < 4 && !String.IsNullOrWhiteSpace(prereleasePart))
        {
            MatchCollection prereleaseNumbers = Regex.Matches(prereleasePart, "\\d+");
            if (prereleaseNumbers.Count > 0)
            {
                String lastNumeric = prereleaseNumbers[prereleaseNumbers.Count - 1].Value;
                if (Int32.TryParse(lastNumeric, out Int32 prereleaseNumeric))
                    versionNumbers.Add(prereleaseNumeric);
            }
        }

        if (versionNumbers.Count > 4)
            versionNumbers = versionNumbers.GetRange(0, 4);

        if (versionNumbers.TrueForAll(v => v == 0))
            messages.Add(new ParserMessage($"The version '{rawVersion}' must contain at least one non-zero number.", ParserMessage.MessageType.Error, (0u, 0u)));

        return String.Join(".", versionNumbers);
    }

    public CreateOutputResult ToLanguageConfig(MessageContext messageContext)
    {
        List<ParserMessage> messages = new List<ParserMessage>();
        VSCodeLanguageConfDocument document = new VSCodeLanguageConfDocument();
        Boolean successful = false;
        try
        {
            String GetTextValueOfDefinition(Definition definition)
            {
                AbstractDefinitionElement definitionElement = definition.DefinitionElement;
                while (definitionElement is not TextTerminal)
                {
                    if (definitionElement is AbstractMarker marker)
                        definitionElement = marker.Element ?? throw new Exception("Element required!");
                    else
                        throw GetException("Quoted definitions can only include literals!");
                }
                return (definitionElement as TextTerminal)!.AsText().ReplaceAll(new (String Search, String Replace)[] { ("\\'", "'"), ("\\\"", "\""), ("\\\\", "\\") });
            }
            List<KeyValuePair<String, String>> brackets = new List<KeyValuePair<String, String>>();
            List<KeyValuePair<String, String>> autoClosingPairs = new List<KeyValuePair<String, String>>();
            List<KeyValuePair<String, String>> surroundingPairs = new List<KeyValuePair<String, String>>();
            foreach (Definition definition in Definitions)
            {
                if (definition.KeyValuePairs.ContainsKey(Grammar.GrammarPropertyQuote))
                {
                    String quoteValue = GetTextValueOfDefinition(definition);
                    autoClosingPairs.Add(new KeyValuePair<String, String>(quoteValue, quoteValue));
                    surroundingPairs.Add(new KeyValuePair<String, String>(quoteValue, quoteValue));
                }
                if (definition.KeyValuePairs.TryGetValue(Grammar.GrammarPropertyBracketOpen, out String bracketIdentifier))
                {
                    Definition? correspondingDefinition = Definitions
                        .Where(d => (d != definition) && d.KeyValuePairs.TryGetValue(Grammar.GrammarPropertyBracketClose, out String closeBracketValue) && (closeBracketValue == bracketIdentifier))
                        .FirstOrDefault();
                    String? closeBracket = correspondingDefinition != null ? GetTextValueOfDefinition(correspondingDefinition) : null;
                    if (!String.IsNullOrEmpty(closeBracket))
                    {
                        String openBracket = GetTextValueOfDefinition(definition);
                        brackets.Add(new KeyValuePair<String, String>(openBracket, closeBracket!));
                        autoClosingPairs.Add(new KeyValuePair<String, String>(openBracket, closeBracket!));
                        surroundingPairs.Add(new KeyValuePair<String, String>(openBracket, closeBracket!));
                    }
                    else
                        throw GetException($"A closing bracket for \"{Grammar.GrammarPropertyBracketOpen}: {bracketIdentifier}\" is required!");
                }
            }
            String? lineComment = TryGetOptionValue(Grammar.TextMateOptionLineComment);
            KeyValuePair<String, String>? blockComment = null;
            document = new VSCodeLanguageConfDocument
            {
                Comments = new VSCodeLanguageConfComments
                {
                    LineComment = lineComment,
                    BlockComment = blockComment
                },
                Brackets = brackets,
                AutoClosingPairs = autoClosingPairs,
                SurroundingPairs = surroundingPairs
            };
            successful = true;
        }
        catch (GrammarException e)
        {
            messages.Add(new ParserMessage(e.Message, ParserMessage.MessageType.Error, (e.Row, e.Column)));
        }

        JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new KeyValuePairArrayConverter() }
        };
        return new CreateOutputResult(successful, JsonSerializer.Serialize(document, serializerOptions), messages);
    }

    public CreateOutputResult ToVSCodePackage(MessageContext messageContext, String? versionOverride = null, Func<String, String>? loadMergeJson = null, String? packageJsonOverridePath = null)
    {
        List<ParserMessage> messages = new List<ParserMessage>();
        VSCodePackageDocument document = new VSCodePackageDocument();
        Boolean successful = false;
        String packageJson = String.Empty;
        try
        {
            String languageDisplayName = GetOptionValue(Grammar.TextMateOptionDisplayName);
            String languageName = (TryGetOptionValue(Grammar.TextMateOptionLanguageName) ?? languageDisplayName).ToLower().Replace(" ", "");
            String normalizedVersion = NormalizeMarketplaceVersion(versionOverride ?? GetOptionValue(Grammar.TextMateOptionVersion), messages);

            document = new VSCodePackageDocument
            {
                Name = languageName,
                DisplayName = languageDisplayName,
                Version = normalizedVersion,
                Contributes =
                    new VSCodePackageContributes
                    {
                        Languages = ImmutableArray.Create<VSCodePackageLanguage>().Add(
                            new VSCodePackageLanguage
                            {
                                Id = languageName,
                                Aliases = ImmutableArray.Create<String>().Add(languageDisplayName).Add(languageName),
                                Extensions = GetFileTypes()
                            }
                        ),
                        Grammars = ImmutableArray.Create<VSCodePackageGrammar>().Add(
                            new VSCodePackageGrammar
                            {
                                Language = languageName,
                                ScopeName = TryGetOptionValue(Grammar.TextMateOptionScopeName) ?? $"source.{languageName}",
                                Path = $"./syntaxes/{languageName}.tmLanguage.json"
                            }
                        )
                    }
            };
            successful = !messages.Exists(m => m.Type == ParserMessage.MessageType.Error);
        }
        catch (GrammarException e)
        {
            messages.Add(new ParserMessage(e.Message, ParserMessage.MessageType.Error, (e.Row, e.Column)));
        }
        JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        if (successful)
        {
            packageJson = JsonSerializer.Serialize(document, serializerOptions);
            String? mergePath = packageJsonOverridePath ?? TryGetOptionValue(Grammar.VSCodeOptionPackageJsonMerge);
            if (!String.IsNullOrWhiteSpace(mergePath))
            {
                try
                {
                    if (loadMergeJson is null)
                        throw new InvalidOperationException("No merge callback provided.");

                    String overrideContent = loadMergeJson.Invoke(mergePath!);
                    packageJson = MergePackageJson(packageJson, overrideContent);
                }
                catch (Exception ex)
                {
                    successful = false;
                    messages.Add(new ParserMessage($"Failed to merge package override '{mergePath}': {ex.Message}", ParserMessage.MessageType.Error, (0u, 0u)));
                }
            }
        }

        return new CreateOutputResult(successful, packageJson, messages);
    }

    public CreateOutputResult ToParserCode(MessageContext messageContext)
    {
        List<ParserMessage> messages = new List<ParserMessage>();
        String result = String.Empty;
        Boolean successful = false;
        try
        {
            AddUnknownIdentifierWarnings(messages);
            AddUnusedDefinitionWarnings(messages);
            Boolean generateNodeVisitor = ShouldGenerateNodeVisitor();
            result =
                $$"""
                #nullable enable

                using System.Text;
                using System.Text.RegularExpressions;

                namespace {{GetOptionValue(Grammar.GrammarOptionNamespace)}}
                {
                {{Indent(GetIVisitorCode(generateNodeVisitor))}}

                {{Indent(GetParseResultCode(generateNodeVisitor))}}

                {{Indent(GetGlobalClassesCode())}}

                    public sealed class {{GetOptionValue(Grammar.GrammarOptionClass)}}
                    {
                {{Indent(Indent(GetParseCode()))}}

                {{Indent(Indent(GetBasicCode()))}}

                {{Indent(Indent(GetCheckDefinitionCode()))}}
                    }
                }
                """.TrimLineEndWhitespace();
            successful = true;
        }
        catch (GrammarException e)
        {
            messages.Add(new ParserMessage(e.Message, ParserMessage.MessageType.Error, (e.Row, e.Column)));
        }
        return new CreateOutputResult(successful, result, messages);
    }

    private IReadOnlyDictionary<String, TMDefinition.TextMateRepositoryEntry> GetTextMateRepository(Grammar grammar, IList<ParserMessage> messages)
    {
        var result = new Dictionary<String, TMDefinition.TextMateRepositoryEntry>(StringComparer.OrdinalIgnoreCase);
        List<TMDefinition> tmDefinitions = TMDefinitions.ToList();
        String grammarSuffix = grammar.GetGrammarSuffix();
        foreach (var definition in Definitions.Where(d => d.KeyValuePairs.ContainsKey(TextMatePropertyPattern)))
        {
            foreach (TMDefinition tmDefinition in tmDefinitions.Where(td => td.Name.Equals(definition.Name, StringComparison.OrdinalIgnoreCase)))
                throw GetException($"TextMate definition '{definition.Name}' already exists!");
            String? scopeName = definition.KeyValuePairs[TextMatePropertyPattern];
            scopeName = String.IsNullOrEmpty(scopeName) ? null : scopeName;
            TMSequence sequence = new TMSequence(new List<AbstractDefinitionElement>() { definition.DefinitionElement }, definition.MessageContext, definition.Node);
            tmDefinitions.Add(new TMDefinition(definition.Name, scopeName, sequence, null, null, definition.MessageContext, definition.Node));
        }
        foreach (TMDefinition tmDefinition in tmDefinitions)
            AddScopeOverrideWarnings(tmDefinition, grammarSuffix, messages);
        HashSet<String> referencedDefinitionNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase)
        {
            GetTMRootDefinition().Name
        };
        foreach (TMDefinition tmDefinition in tmDefinitions)
        {
            if (tmDefinition.Includes is null)
                continue;
            foreach (ReferenceElement include in tmDefinition.Includes.Includes)
            {
                if (!include.ReferenceName.Equals(tmDefinition.Name, StringComparison.OrdinalIgnoreCase))
                    referencedDefinitionNames.Add(include.ReferenceName);
            }
        }
        foreach (TMDefinition tmDefinition in tmDefinitions)
        {
            if (referencedDefinitionNames.Contains(tmDefinition.Name))
                result[tmDefinition.Name.ToLower()] = tmDefinition.GetRepositoryEntry(grammar);
            else
            {
                (UInt32 row, UInt32 column) = tmDefinition.MessageContext.CalculateLocation(tmDefinition.Node.Position);
                messages.Add(new ParserMessage($"TextMate definition '{tmDefinition.Name}' is not referenced by any other rule.", ParserMessage.MessageType.Warning, (row, column)));
            }
        }
        return result;
    }

    private String[] GetFileTypes()
    {
        String rawValue = GetOptionValue(Grammar.TextMateOptionFileType);
        String[] parts = rawValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0).ToArray(); ;
        return parts.Length == 0 ? new[] { rawValue.Trim() } : parts;
    }

    public override bool MatchesVariableText()
    {
        Boolean result = false;
        foreach (Definition definition in Definitions)
            result = result || definition.MatchesVariableText();
        return result;
    }

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
            foreach (Definition definition in Definitions)
                definition.IterateElements(process);
    }

    public Definition? FindDefinitionByName(String name)
    {
        return Definitions.FirstOrDefault(element => element.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }

    public TMDefinition? FindTMDefinitionByName(String name)
    {
        return TMDefinitions.FirstOrDefault(element => element.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }

    public void CheckDuplicatedDefinitions(List<Definition> definitions)
    {
        var duplicates = definitions
            .GroupBy(d => d.Name, StringComparer.InvariantCultureIgnoreCase)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g);

        foreach (Definition definition in duplicates)
            throw definition.GetException($"Definition '{definition.Name}' already exists!");
    }

    public Int32 GetElementIdOf(AbstractNamedElement element)
    {
        if ((element is Definition) && (Definitions.IndexOf((Definition)element) >= 0))
            return Definitions.IndexOf((Definition)element);
        throw GetException($"Can not find identifier '{element.Name}'!");
    }

    public Definition GetRootDefinition()
    {
        String? rootName = GetOptionValue(Grammar.GrammarOptionRoot);
        if (String.IsNullOrWhiteSpace(rootName))
            throw GetException("Grammar must have root option!");
        Definition? definition = FindDefinitionByName(rootName);
        if (definition is null)
            throw GetException($"Can not find root definition '{rootName}'!");
        return definition;
    }

    public TMDefinition GetTMRootDefinition()
    {
        String? rootName = GetOptionValue(Grammar.GrammarOptionRoot);
        if (String.IsNullOrWhiteSpace(rootName))
            throw GetException("Grammar must have root option!");
        TMDefinition? definition = FindTMDefinitionByName($"{rootName}");
        if (definition is null)
            throw GetException($"Can not find TextMate root definition '!{rootName}'!");
        return definition;
    }

    private String GetOptionValue(String key) => TryGetOptionValue(key) ?? throw GetException($"Can not find option '{key}'!");

    private String? TryGetOptionValue(String key)
    {
        return Options
            .Where(value => value.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
            .Select(value => value.Value)
            .FirstOrDefault();
    }

    internal String GetGrammarSuffix()
    {
        String scopeName = GetOptionValue(Grammar.TextMateOptionScopeName);
        // Extract the last part after the last dot (e.g., "parseidon" from "source.parseidon")
        Int32 lastDotIndex = scopeName.LastIndexOf('.');
        return lastDotIndex >= 0 ? scopeName.Substring(lastDotIndex + 1) : scopeName;
    }

    internal static String? AppendGrammarSuffix(String? scopeName, String grammarSuffix)
    {
        if (String.IsNullOrEmpty(scopeName))
            return scopeName;
        // Prüfen, ob der Scope-Name bereits mit dem Grammar-Suffix endet
        if (scopeName!.EndsWith($".{grammarSuffix}", StringComparison.OrdinalIgnoreCase))
            return scopeName;
        return $"{scopeName}.{grammarSuffix}";
    }

    private void AddScopeOverrideWarnings(TMDefinition tmDefinition, String grammarSuffix, IList<ParserMessage> messages)
    {
        tmDefinition.IterateElements(element =>
        {
            if (element is TMSequence sequence && !String.IsNullOrWhiteSpace(sequence.ScopeName) && sequence.Elements.Count == 1)
            {
                String scopedSequenceName = AppendGrammarSuffix(sequence.ScopeName, grammarSuffix) ?? sequence.ScopeName!;
                String? innerScope = TryGetElementScope(sequence.Elements.First(), grammarSuffix);
                if (!String.IsNullOrWhiteSpace(innerScope) && !scopedSequenceName.Equals(innerScope, StringComparison.OrdinalIgnoreCase))
                {
                    (UInt32 row, UInt32 column) = tmDefinition.MessageContext.CalculateLocation(sequence.Elements.First().Node.Position);
                    messages.Add(new ParserMessage($"TextMate scope '{scopedSequenceName}' overrides inner scope '{innerScope}' in rule '{tmDefinition.Name}'.", ParserMessage.MessageType.Warning, (row, column)));
                }
            }
            return true;
        });
    }

    private String? TryGetElementScope(AbstractDefinitionElement element, String grammarSuffix)
    {
        switch (element)
        {
            case TMSequence sequence when !String.IsNullOrWhiteSpace(sequence.ScopeName):
                return AppendGrammarSuffix(sequence.ScopeName, grammarSuffix) ?? sequence.ScopeName;
            case ReferenceElement referenceElement:
                Definition? referencedDefinition = FindDefinitionByName(referenceElement.ReferenceName);
                if (referencedDefinition is not null && referencedDefinition.KeyValuePairs.TryGetValue(TextMatePropertyScope, out String scopeName))
                    return AppendGrammarSuffix(scopeName, grammarSuffix) ?? scopeName;
                break;
        }
        return null;
    }

    private void AddUnusedDefinitionWarnings(IList<ParserMessage> messages)
    {
        String rootName = GetOptionValue(GrammarOptionRoot);
        HashSet<String> referencedDefinitions = new HashSet<String>(StringComparer.OrdinalIgnoreCase)
        {
            rootName
        };

        IterateElements(element =>
        {
            if (element is ReferenceElement referenceElement)
            {
                Definition? parentDefinition = GetParentDefinition(referenceElement);
                if ((parentDefinition is null) || !parentDefinition.Name.Equals(referenceElement.ReferenceName, StringComparison.OrdinalIgnoreCase))
                    referencedDefinitions.Add(referenceElement.ReferenceName);
            }
            return true;
        });

        foreach (Definition definition in Definitions)
        {
            if (referencedDefinitions.Contains(definition.Name))
                continue;

            (UInt32 row, UInt32 column) = definition.MessageContext.CalculateLocation(definition.Node.Position);
            messages.Add(new ParserMessage($"Definition '{definition.Name}' is not referenced by any other rule.", ParserMessage.MessageType.Warning, (row, column)));
        }
    }

    private Definition? GetParentDefinition(AbstractGrammarElement element)
    {
        AbstractGrammarElement? parent = element.Parent;
        while (parent is not null)
        {
            if (parent is Definition definition)
                return definition;
            parent = parent.Parent;
        }
        return null;
    }

    private void AddUnknownIdentifierWarnings(IList<ParserMessage> messages)
    {
        HashSet<String> knownOptions = new HashSet<String>(StringComparer.OrdinalIgnoreCase)
        {
            GrammarOptionNamespace,
            GrammarOptionClass,
            GrammarOptionRoot,
            GrammarOptionNoInterface,
            TextMateOptionDisplayName,
            TextMateOptionScopeName,
            TextMateOptionFileType,
            TextMateOptionLanguageName,
            TextMateOptionVersion,
            TextMateOptionLineComment,
            VSCodeOptionPackageJsonMerge
        };

        HashSet<String> knownProperties = new HashSet<String>(StringComparer.OrdinalIgnoreCase)
        {
            TextMatePropertyScope,
            TextMatePropertyPattern,
            GrammarPropertyErrorName,
            GrammarPropertyQuote,
            GrammarPropertyBracketOpen,
            GrammarPropertyBracketClose
        };

        foreach (ValuePair option in Options)
        {
            if (!knownOptions.Contains(option.Name))
            {
                (UInt32 row, UInt32 column) = option.MessageContext.CalculateLocation(option.Node.Position);
                messages.Add(new ParserMessage($"Unknown option '{option.Name}'.", ParserMessage.MessageType.Warning, (row, column)));
            }
        }

        foreach (Definition definition in Definitions)
        {
            foreach (ValuePair property in definition.ValuePairs)
            {
                if (!knownProperties.Contains(property.Name))
                {
                    (UInt32 row, UInt32 column) = property.MessageContext.CalculateLocation(property.Node.Position);
                    messages.Add(new ParserMessage($"Unknown property '{property.Name}' in definition '{definition.Name}'.", ParserMessage.MessageType.Warning, (row, column)));
                }
            }
        }
    }

    private Boolean ShouldGenerateNodeVisitor()
    {
        return !Options.Any(option => option.Name.Equals(GrammarOptionNoInterface, StringComparison.OrdinalIgnoreCase));
    }

    private void CheckTreatInlineCycles()
    {
        Dictionary<Definition, List<Definition>> references = new Dictionary<Definition, List<Definition>>();
        foreach (Definition definition in Definitions)
        {
            List<Definition> refs = new List<Definition>();
            definition.IterateElements(element =>
            {
                if (element is ReferenceElement referenceElement)
                {
                    Definition? referencedDefinition = FindDefinitionByName(referenceElement.ReferenceName);
                    if (referencedDefinition is not null)
                        refs.Add(referencedDefinition);
                }
                return true;
            });
            references[definition] = refs;
        }

        Dictionary<Definition, Int32> state = Definitions.ToDictionary(d => d, _ => 0);
        Stack<Definition> stack = new Stack<Definition>();

        void Visit(Definition definition)
        {
            if (state[definition] == 2)
                return;
            if (state[definition] == 1)
                return;

            state[definition] = 1;
            stack.Push(definition);
            foreach (Definition referenced in references[definition])
            {
                if (state[referenced] == 1)
                {
                    if (!referenced.HasMarker<TreatInlineMarker>())
                        continue;

                    List<Definition> path = stack.Reverse().ToList(); // root -> current
                    List<Definition> cycle = path.SkipWhile(d => d != referenced).ToList();
                    cycle.Add(referenced);

                    (UInt32 row, UInt32 column) = referenced.MessageContext.CalculateLocation(referenced.Node.Position);
                    throw GetException($"Circular reference involving TreatInline definition '{referenced.Name}' detected: {String.Join(" -> ", cycle.Select(d => d.Name))}");
                }
                else if (state[referenced] == 0)
                    Visit(referenced);
            }
            stack.Pop();
            state[definition] = 2;
        }

        foreach (Definition definition in Definitions)
            Visit(definition);
    }

    private Boolean IterateUsedDefinitions(AbstractGrammarElement element, List<Definition> definitions)
    {
        if ((element is Definition definition) && (definitions.IndexOf(definition) < 0) && !definition.HasMarker<TreatInlineMarker>())
            definitions.Add(definition);
        else
            if ((element is ReferenceElement referenceElement) && (!referenceElement.TreatReferenceInline) && (FindDefinitionByName(referenceElement.ReferenceName) is Definition referencedDefinition) && (definitions.IndexOf(referencedDefinition) < 0))
                referencedDefinition.IterateElements((element) => IterateUsedDefinitions(element, definitions));
        return true;
    }

    private List<Definition> GetUsedDefinitions()
    {
        List<Definition> result = new List<Definition>();
        Definition rootDefinition = GetRootDefinition();
        result.Add(rootDefinition);
        rootDefinition.IterateElements((element) => IterateUsedDefinitions(element, result));
        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return result;
    }

    private Boolean IterateRelevantGrammarDefinitions(AbstractGrammarElement element, List<Definition> definitions, Boolean forceAdd)
    {
        if ((element is Definition definition) && (definitions.IndexOf(definition) < 0) && !(definition.DropDefinition) && (definition.MatchesVariableText() || forceAdd))
            definitions.Add(definition);
        else
            if ((element is ReferenceElement referenceElement) && (!referenceElement.TreatReferenceInline) && (FindDefinitionByName(referenceElement.ReferenceName) is Definition referencedDefinition) && (definitions.IndexOf(referencedDefinition) < 0))
            {
                Boolean hasDropMarker = false;
                AbstractGrammarElement? parent = element.Parent;
                while ((parent is not null) && !hasDropMarker)
                {
                    hasDropMarker = parent is DropMarker;
                    parent = parent.Parent;
                }
                if (!hasDropMarker)
                {
                    Boolean hasOrParent = false;
                    parent = element.Parent;
                    while ((parent is not null) && !hasOrParent)
                    {
                        hasOrParent = parent is OrOperator;
                        parent = parent.Parent;
                    }
                    Boolean hasOptionalParent = false;
                    if (!hasOrParent)
                    {
                        parent = element.Parent;
                        while ((parent is not null) && !hasOptionalParent)
                        {
                            hasOptionalParent = parent is OptionalOperator;
                            parent = parent.Parent;
                        }
                    }

                    referencedDefinition.IterateElements(
                        (element) => IterateRelevantGrammarDefinitions(element, definitions, hasOrParent || hasOptionalParent)
                    );
                }
            }
        Boolean result = !((element is Definition definition1) && (definition1.HasMarker<IsTerminalMarker>()));
        return result;
    }

    private List<Definition> GetRelevantGrammarDefinitions()
    {
        List<Definition> result = new List<Definition>();
        Definition rootDefinition = GetRootDefinition();
        result.Add(rootDefinition);
        rootDefinition.IterateElements((element) => IterateRelevantGrammarDefinitions(element, result, false));
        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return result;
    }

    protected String GetCheckDefinitionCode()
    {
        StringBuilder builder = new StringBuilder();
        foreach (Definition definition in GetUsedDefinitions())
        {
            String definitionCode =
                $$"""
                private Boolean CheckDefinition_{{definition.Name}}(ASTNode parentNode, ParserState state, String? errorName)
                {
                    Int32 oldPosition = state.Position;
                    ASTNode actualNode = new ASTNode({{GetElementIdOf(definition)}}, "{{definition.Name}}", "", state.Position);
                    Boolean result = {{GetElementsCode(new List<Definition>() { definition }, null)}}
                    Int32 foundPosition = state.Position;
                    if (result && ((actualNode.Children.Count > 0) || (actualNode.Text != "")))
                        parentNode.AddChild(actualNode);
                    return result;
                }

                """;

            builder.AppendLine(definitionCode);
        }
        return builder.ToString();
    }

    protected String GetParseResultCode(Boolean generateNodeVisitor)
    {
        String GetEventName(Definition definition) => $"Process{definition.Name.Humanize().Dehumanize()}Node";
        String visitorCalls = String.Empty;
        if (generateNodeVisitor)
        {
            List<Definition> usedDefinitions = GetRelevantGrammarDefinitions();
            StringBuilder visitorCallsBuilder = new StringBuilder();
            foreach (Definition definition in usedDefinitions)
                visitorCallsBuilder.AppendLine($"case {GetElementIdOf(definition)}: return visitor.{GetEventName(definition)}(context, node, messages);");
            visitorCalls = visitorCallsBuilder.ToString();
        }

        String nodeVisitorMethods = String.Empty;
        if (generateNodeVisitor)
        {
            nodeVisitorMethods =
                $$"""

                private ProcessNodeResult DoVisit(Object context, INodeVisitor visitor, ASTNode node, IList<ParserMessage> messages)
                {
                    visitor.BeginVisit(context, node);
                    try
                    {
                        if (node == null)
                            return ProcessNodeResult.Error;
                        Boolean result = true;
                        foreach (ASTNode child in node.Children)
                            result = result && (DoVisit(context, visitor, child, messages) == ProcessNodeResult.Success);
                        result = result && (CallEvent(context, visitor, node.TokenId, node, messages) == ProcessNodeResult.Success);
                        return result ? ProcessNodeResult.Success : ProcessNodeResult.Error;
                    }
                    finally
                    {
                        visitor.EndVisit(context, node);
                    }
                }

                private ProcessNodeResult CallEvent(Object context, INodeVisitor visitor, Int32 tokenId, ASTNode node, IList<ParserMessage> messages)
                {
                    switch (tokenId)
                    {
                {{Indent(Indent(visitorCalls))}}
                    }
                    return ProcessNodeResult.Success;
                }
                """;
        }

        String result =
            $$"""
            public sealed class MessageContext
            {
                private String _text;
                internal MessageContext(String text)
                {
                    _text = text;
                }

                public (UInt32 Row, UInt32 Column) CalculateLocation(Int32 position)
                {
                    Int32 row = 1;
                    Int32 column = 1;
                    Int32 limit = position;
                    if (limit > _text.Length)
                        limit = _text.Length;

                    for (Int32 index = 0; index < limit; index++)
                    {
                        if (_text[index] == '\n')
                        {
                            row++;
                            column = 1;
                        }
                        else
                        {
                            column++;
                        }
                    }

                    return ((UInt32)row, (UInt32)column);
                }
            }

            public sealed class ParseResult
            {
                private class EmptyResult : IVisitResult
                {
                    public EmptyResult(Boolean successful, IReadOnlyList<ParserMessage> messages)
                    {
                        Successful = successful;
                        Messages = messages;
                    }
                    public Boolean Successful { get; }
                    public IReadOnlyList<ParserMessage> Messages { get; }
                }

                public ParseResult(ASTNode? rootNode, MessageContext messageContext, IReadOnlyList<ParserMessage> messages)
                {
                    RootNode = rootNode;
                    MessageContext = messageContext;
                    Messages = new List<ParserMessage>(messages);
                }

                public Boolean Successful { get => RootNode is not null; }
                public ASTNode? RootNode { get; }
                public IReadOnlyList<ParserMessage> Messages { get; }
                public MessageContext MessageContext { get; }

                public IVisitResult Visit(IVisitor visitor)
                {
                    if (visitor is null)
                        throw new ArgumentNullException(nameof(visitor));
                    List<ParserMessage> visitMessages = new List<ParserMessage>();
                    if (Successful)
                    {
                        try
                        {
                            Object context = visitor.GetContext(this);
                            {{(generateNodeVisitor ? "if (visitor is INodeVisitor)" : "")}}
                                {{(generateNodeVisitor ? "DoVisit(context, (visitor as INodeVisitor)!, RootNode!, visitMessages);" : "")}}
                            return visitor.GetResult(context, true, visitMessages);
                        }
                        catch (GrammarException ex)
                        {
                            visitMessages.Add(new ParserMessage(ex.Message, ParserMessage.MessageType.Error, (ex.Row, ex.Column)));
                        }
                    }
                    return new EmptyResult(false, visitMessages);
                }
            {{Indent(nodeVisitorMethods)}}
            }
            """;
        return result;
    }

    protected String GetParseCode()
    {
        Definition rootDefinition = GetRootDefinition();
        String result =
            $$"""
            public ParseResult Parse(String text)
            {
                ParserState state = new ParserState(text, new MessageContext(text));
                ASTNode actualNode = new ASTNode(-1, "ROOT", "", 0);
                String? errorName = null;
                Boolean successful = {{rootDefinition.GetReferenceCode(this)}} && state.Position >= text.Length - 1;
                if (successful)
                    state.NoError(state.Position);
                return new ParseResult(successful ? actualNode : null, state.MessageContext, state.Messages);
            }
            """;
        return result;
    }

    private String GetElementsCode(IEnumerable<Definition> elements, Definition? separatorTerminal)
    {
        String result = String.Join("", elements.Select(x => x.ToParserCode(this))) + ";";
        if (result.IndexOf("\n") > 0)
            result = $"\n{result}";
        return Indent(Indent(result));
    }

    protected String GetIVisitorCode(Boolean generateNodeVisitor)
    {
        String GetEventName(Definition definition) => $"Process{definition.Name.Humanize().Dehumanize()}Node";
        List<Definition> usedDefinitions = GetRelevantGrammarDefinitions();
        StringBuilder visitorEventsBuilder = new StringBuilder();
        foreach (Definition definition in usedDefinitions)
            visitorEventsBuilder.AppendLine($"ProcessNodeResult {GetEventName(definition)}(Object context, ASTNode node, IList<ParserMessage> messages);");
        String visitorEvents = visitorEventsBuilder.ToString();
        StringBuilder builder = new StringBuilder();
        builder.Append(
            """
            public interface IVisitResult
            {
                Boolean Successful { get; }
                IReadOnlyList<ParserMessage> Messages { get; }
            }

            public interface IVisitor
            {
                Object GetContext(ParseResult parseResult);
                IVisitResult GetResult(Object context, Boolean successful, IReadOnlyList<ParserMessage> messages);
            }

            """
        );
        if (generateNodeVisitor)
        {
            builder.AppendLine(
                $$"""

                public interface INodeVisitor : IVisitor
                {
                {{Indent(visitorEvents)}}
                    void BeginVisit(Object context, ASTNode node);
                    void EndVisit(Object context, ASTNode node);
                }

                public enum ProcessNodeResult
                {
                    Success,
                    Error
                }
                """
            );
        }
        return builder.ToString();
    }

    protected String GetBasicCode()
    {
        String result =
            $$"""
            private sealed class ParserState
            {
                public ParserState(String text, MessageContext messageContext)
                {
                    Text = text;
                    MessageContext = messageContext;
                }

                private readonly List<String> _terminalNames = new List<String>();
                private Int32 _lastErrorPosition = -1;
                private Int32 _lastParserPosition = -1;
                private List<String> _errorExpectations = new List<String>();
                private List<ParserMessage> _messages = new List<ParserMessage>();

                internal String Text { get; }
                internal Int32 Position { get; set; } = 0;
                internal Boolean Eof => !(Position < Text.Length);
                internal MessageContext MessageContext { get; }
                internal IReadOnlyList<ParserMessage> Messages
                {
                    get
                    {
                        var tempMessages = new List<ParserMessage>(_messages);
                        if (_errorExpectations.Count > 0)
                        {
                            String actual = _lastErrorPosition < Text.Length
                                ? $"found {DescribeLiteral(Text.Substring(_lastParserPosition, _lastErrorPosition - _lastParserPosition + 1))}"
                                : "found end of input";                        
                            String errorMessage = $"Expected {String.Join(" or ", _errorExpectations)}, {actual}!";
                            tempMessages.Add(new ParserMessage(errorMessage, ParserMessage.MessageType.Error, MessageContext.CalculateLocation(_lastParserPosition)));
                        }
                        return tempMessages;
                    }
                }

                public void ReportError(String message, Int32 parserPosition, Int32 errorPosition)
                {
                    if ((parserPosition >= _lastParserPosition) && (parserPosition < Text.Length))
                    {
                        if (parserPosition > _lastParserPosition)
                            NoError(parserPosition);
                        if (!_errorExpectations.Contains(message))
                            _errorExpectations.Add(message);
                        _lastParserPosition = parserPosition;
                        if (errorPosition > _lastErrorPosition)
                            _lastErrorPosition = errorPosition;
                    }
                }

                public void NoError(Int32 parserPosition)
                {
                    if (parserPosition >= _lastParserPosition)
                        _errorExpectations.Clear();
                }
            }

            private static String DescribeLiteral(String value)
            {
                if (value.Length == 0)
                    return "\"\"";

                StringBuilder builder = new StringBuilder(value.Length + 2);
                builder.Append('\"');
                foreach (Char character in value)
                {
                    builder.Append(EscapeCharacter(character));
                }
                builder.Append('\"');
                return builder.ToString();
            }

            private static String DescribeCharacter(Char value) => $"\"{EscapeCharacter(value)}\"";

            private static String DescribePattern(String value)
            {
                StringBuilder builder = new StringBuilder(value.Length + 2);
                builder.Append('/');
                foreach (Char character in value)
                {
                    builder.Append(EscapeCharacter(character));
                }
                builder.Append('/');
                return builder.ToString();
            }

            private static String EscapeCharacter(Char value)
            {
                return value switch
                {
                    '\r' => "\\r",
                    '\n' => "\\n",
                    '\t' => "\\t",
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    '\'' => "\\'",
                    _ when Char.IsControl(value) => $"\\x{((Int32)value):X2}",
                    _ => value.ToString()
                };
            }

            private Boolean CheckRegEx(ASTNode parentNode, ParserState state, String? errorName, String regEx, Int32 quantifier)
            {
                Int32 oldPosition = state.Position;
                if ((state.Position < state.Text.Length) && (Regex.Match(state.Text.Substring(state.Position, quantifier), $"{regEx}{{"{{{"}}quantifier{{"}}}"}}") is Match regexMatch) && regexMatch.Success)
                {
                    state.Position += regexMatch.Length;
                    parentNode.AddChild(new ASTNode(-1, "REGEX", state.Text.Substring(oldPosition, state.Position - oldPosition), state.Position));
                    state.NoError(state.Position);
                    return true;
                }

                Int32 failurePosition = state.Position < state.Text.Length ? state.Position : state.Text.Length;
                state.Position = oldPosition;
                state.ReportError(errorName ?? $"input matching regex {DescribePattern(regEx)}", oldPosition, failurePosition);
                return false;
            }

            private Boolean CheckText(ASTNode parentNode, ParserState state, String? errorName, String text)
            {
                Int32 oldPosition = state.Position;
                Int32 position = 0;
                while (position < text.Length)
                {
                    if (state.Eof || (state.Text[state.Position] != text[position]))
                    {
                        Int32 failurePosition = state.Position < state.Text.Length ? state.Position : state.Text.Length;
                        state.Position = oldPosition;
                        state.ReportError(errorName ?? DescribeLiteral(text), oldPosition, failurePosition);
                        return false;
                    }
                    position++;
                    state.Position++;
                }
                parentNode.AddChild(new ASTNode(-1, "TEXT", state.Text.Substring(oldPosition, state.Position - oldPosition), state.Position));
                state.NoError(state.Position);
                return true;
            }

            private Boolean CheckAnd(ASTNode parentNode, ParserState state, String? errorName, Func<ASTNode, String?, Boolean> leftCheck, Func<ASTNode, String?, Boolean> rightCheck)
            {
                Int32 oldPosition = state.Position;
                ASTNode tempNode = new ASTNode(parentNode.TokenId, "AND", parentNode.Text, state.Position);
                tempNode.Position = parentNode.Position;
                if (leftCheck(tempNode, errorName))
                {
                    if (rightCheck(tempNode, errorName))
                    {
                        parentNode.AddChild(tempNode);
                        parentNode.AssignFrom(tempNode);
                        return true;
                    }
                }
                state.Position = oldPosition;
                return false;
            }

            private Boolean CheckOr(ASTNode parentNode, ParserState state, String? errorName, Func<ASTNode, String?, Boolean> leftCheck, Func<ASTNode, String?, Boolean> rightCheck)
            {
                Int32 oldPosition = state.Position;
                if (leftCheck(parentNode, errorName))
                    return true;
                state.Position = oldPosition;
                return rightCheck(parentNode, errorName);
            }

            private Boolean Drop(ASTNode parentNode, ParserState state, String? errorName, Func<ASTNode, String?, Boolean> check)
            {
                ASTNode tempNode = new ASTNode(-1, "", "", state.Position);
                return check(tempNode, errorName);
            }

            private Boolean CheckOneOrMore(ASTNode parentNode, ParserState state, String? errorName, Func<ASTNode, String?, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                if (!check(parentNode, errorName))
                    return false;

                oldPosition = state.Position;
                while (!state.Eof)
                {
                    Int32 snapshot = state.Position;
                    if (!check(parentNode, errorName))
                    {
                        state.Position = snapshot;
                        break;
                    }
                    if (state.Position == snapshot)
                        break;
                    oldPosition = state.Position;
                }
                state.Position = oldPosition;
                return true;
            }

            private Boolean CheckZeroOrMore(ASTNode parentNode, ParserState state, String? errorName, Func<ASTNode, String?, Boolean> check)
            {
                Int32 lastSuccessfulPosition = state.Position;
                while ((!state.Eof))
                {
                    Int32 snapshot = state.Position;
                    if (!check(parentNode, errorName))
                    {
                        state.Position = snapshot;
                        break;
                    }
                    if (state.Position == snapshot)
                        break;
                    lastSuccessfulPosition = state.Position;
                }
                state.Position = lastSuccessfulPosition;
                return true;
            }

            private Boolean CheckRange(ASTNode parentNode, ParserState state, String? errorName, Int32 minCount, Int32 maxCount, Func<ASTNode, String?, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                Int32 count = 0;

                while (count < minCount)
                {
                    if (!check(parentNode, errorName))
                    {
                        state.Position = oldPosition;
                        return false;
                    }
                    count++;
                    oldPosition = state.Position;
                }

                while ((count < maxCount) && !state.Eof)
                {
                    Int32 snapshot = state.Position;
                    if (!check(parentNode, errorName))
                    {
                        state.Position = snapshot;
                        break;
                    }
                    if (state.Position == snapshot)
                        break;
                    count++;
                    oldPosition = state.Position;
                }

                state.Position = oldPosition;
                return true;
            }

            private Boolean MakeTerminal(ASTNode parentNode, ParserState state, String? errorName, Boolean doNotEscape, Func<ASTNode, String?, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                ASTNode tempNode = new ASTNode(-1, "", "", state.Position);
                Boolean result = check(tempNode, errorName);
                if (result)
                {
                    tempNode.Text = tempNode.GetText();
                    tempNode.ClearChildren();
                    parentNode.Text = tempNode.GetText();
                    if (doNotEscape)
                        parentNode.Text = Regex.Unescape(parentNode.Text);
                }
                return result;
            }

            private Boolean SetErrorName(ASTNode parentNode, ParserState state, String? errorName, Func<ASTNode, String?, Boolean> check)
            {
                return check(parentNode, errorName);
            }

            """;
        return result;
    }

    protected String GetGlobalClassesCode()
    {
        String result =
            $$"""
            public class GrammarException : Exception
            {
                public GrammarException(String message, UInt32 row, UInt32 column) : base(message)
                {
                    Row = row;
                    Column = column;
                }

                public GrammarException(String message, (UInt32 row, UInt32 column) position) : base(message)
                {
                    Row = position.row;
                    Column = position.column;
                }

                public UInt32 Row { get; }
                public UInt32 Column { get; }
            }

            public sealed class ASTNode
            {
                private List<ASTNode> _children { get; } = new List<ASTNode>();
                private ASTNode? _parent = null;

                public String Text { get; internal set; }
                public String Name { get; private set; }
                public IReadOnlyList<ASTNode> Children { get => _children; }
                public Int32 TokenId { get; private set; }
                public Int32 Position { get; internal set; }
                public ASTNode? Parent { get => _parent; }

                internal ASTNode(Int32 tokenId, String name, String text, Int32 position)
                {
                    Text = text;
                    TokenId = tokenId;
                    Name = name;
                    Position = position;
                }

                internal void AssignFrom(ASTNode node)
                {
                    Int32 nodeIndex = _children.IndexOf(node);
                    if (nodeIndex >= 0)
                    {
                        Text = node.Text;
                        TokenId = node.TokenId;
                        Position = node.Position;
                        List<ASTNode> tempChildren = new List<ASTNode>(node.Children);
                        foreach (ASTNode child in tempChildren)
                        {
                            child.SetParent(this, nodeIndex);
                            nodeIndex++;
                        }
                        _children.Remove(node);
                    }
                }
                 
                internal String GetText()
                {
                    if (Children.Count > 0)
                        return String.Join("", Children.Select(x => x.GetText()));
                    else
                        return Text;
                }

                internal void SetParent(ASTNode? parent, Int32 index = -1)
                {
                    if (_parent != null)
                        _parent._children.Remove(this);
                    _parent = parent;
                    if (_parent != null)
                    {
                        if (index < 0)
                            _parent._children.Add(this);
                        else
                            _parent._children.Insert(index, this);
                    }
                }
                
                internal void AddChild(ASTNode? child)
                {
                    if (child != null)
                        _children.Add(child);
                }
            
                internal void ClearChildren()
                {
                    _children.Clear();
                }
            }

            public sealed class ParserMessage
            {
                public enum MessageType
                {
                    Warning,
                    Error
                }

                public ParserMessage(String message, MessageType type, (UInt32 row, UInt32 column) position)
                {
                    Message = message;
                    Row = position.row;
                    Column = position.column;
                    Type = type;
                }

                public String Message { get; }
                public UInt32 Row { get; }
                public UInt32 Column { get; }
                public MessageType Type { get; }
            }
            """;
        return result;
    }

    private sealed class TextMateGrammarDocument
    {
        [JsonPropertyName("displayName")]
        public String DisplayName { get; set; } = String.Empty;

        [JsonPropertyName("scopeName")]
        public String ScopeName { get; set; } = String.Empty;

        [JsonPropertyName("fileTypes")]
        public IReadOnlyList<String> FileTypes { get; set; } = Array.Empty<String>();

        [JsonPropertyName("patterns")]
        public IReadOnlyList<TMDefinition.TextMatePatternInclude> Patterns { get; set; } = Array.Empty<TMDefinition.TextMatePatternInclude>();

        [JsonPropertyName("repository")]
        public IReadOnlyDictionary<String, TMDefinition.TextMateRepositoryEntry> Repository { get; set; } = new Dictionary<String, TMDefinition.TextMateRepositoryEntry>();
    }

    private sealed class VSCodeLanguageConfDocument
    {
        [JsonPropertyName("comments")]
        public VSCodeLanguageConfComments Comments { get; set; } = new VSCodeLanguageConfComments();

        [JsonPropertyName("brackets")]
        public IList<KeyValuePair<String, String>> Brackets { get; set; } = Array.Empty<KeyValuePair<String, String>>();

        [JsonPropertyName("autoClosingPairs")]
        public IList<KeyValuePair<String, String>> AutoClosingPairs { get; set; } = Array.Empty<KeyValuePair<String, String>>();

        [JsonPropertyName("surroundingPairs")]
        public IList<KeyValuePair<String, String>> SurroundingPairs { get; set; } = Array.Empty<KeyValuePair<String, String>>();
    }

    private sealed class VSCodeLanguageConfComments
    {
        [JsonPropertyName("lineComment")]
        public String? LineComment { get; set; }

        [JsonPropertyName("blockComment")]
        public KeyValuePair<String, String>? BlockComment { get; set; }
    }

    private sealed class VSCodePackageDocument
    {
        [JsonPropertyName("name")]
        public String Name { get; set; } = String.Empty;

        [JsonPropertyName("displayName")]
        public String DisplayName { get; set; } = String.Empty;

        [JsonPropertyName("version")]
        public String Version { get; set; } = String.Empty;

        [JsonPropertyName("contributes")]
        public VSCodePackageContributes Contributes { get; set; } = new VSCodePackageContributes();
    }

    private sealed class VSCodePackageContributes
    {
        [JsonPropertyName("languages")]
        public IReadOnlyList<VSCodePackageLanguage> Languages { get; set; } = Array.Empty<VSCodePackageLanguage>();

        [JsonPropertyName("grammars")]
        public IReadOnlyList<VSCodePackageGrammar> Grammars { get; set; } = Array.Empty<VSCodePackageGrammar>();
    }

    private sealed class VSCodePackageLanguage
    {
        [JsonPropertyName("id")]
        public String Id { get; set; } = String.Empty;

        [JsonPropertyName("aliases")]
        public IReadOnlyList<String> Aliases { get; set; } = Array.Empty<String>();

        [JsonPropertyName("extensions")]
        public IReadOnlyList<String> Extensions { get; set; } = Array.Empty<String>();

        [JsonPropertyName("configuration")]
        public String Configuration { get; set; } = "./language-configuration.json";
    }

    private sealed class VSCodePackageGrammar
    {
        [JsonPropertyName("language")]
        public String Language { get; set; } = String.Empty;

        [JsonPropertyName("scopeName")]
        public String ScopeName { get; set; } = String.Empty;

        [JsonPropertyName("path")]
        public String Path { get; set; } = String.Empty;
    }

    private sealed class KeyValuePairArrayConverter : JsonConverter<KeyValuePair<String, String>>
    {
        public override KeyValuePair<String, String> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, KeyValuePair<String, String> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(value.Key);
            writer.WriteStringValue(value.Value);
            writer.WriteEndArray();
        }
    }

    public sealed record CreateOutputResult
    {
        public CreateOutputResult(Boolean successful, String result, IReadOnlyList<ParserMessage> messages)
        {
            Successful = successful;
            Output = result;
            Messages = messages;
        }
        public static CreateOutputResult Empty => new CreateOutputResult(false, "", new List<ParserMessage>());
        public Boolean Successful { get; }
        public String Output { get; }
        public IReadOnlyList<ParserMessage> Messages { get; }
    }
}
