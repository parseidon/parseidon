using System.Text;
using System.Text.Json;
using Humanizer;
using Parseidon.Helper;
using Parseidon.Parser.Grammar.Terminals;
using Parseidon.Parser.Grammar.Block;
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
    }

    public List<Definition> Definitions { get; }
    public List<TMDefinition> TMDefinitions { get; }
    public List<ValuePair> Options { get; }

    public String ParserCode { get => ToParserCode(this); }
    public String LanguageConfig { get => ToLanguageConfig(); }
    public String Package { get => ToPackage(); }

    internal const String GrammarPropertyErrorName = "errorname";
    internal const String TextMatePropertyScope = "tmscope";
    internal const String TextMatePropertyPattern = "tmpattern";

    public String ToTextMateGrammar(MessageContext messageContext)
    {
        TMDefinition rootDefinition = GetTMRootDefinition();

        TextMateGrammarDocument document = new TextMateGrammarDocument
        {
            DisplayName = GetOptionValue("displayname"),
            ScopeName = GetOptionValue("scopename"),
            FileTypes = GetFileTypes(),
            Patterns = new List<TMDefinition.TextMatePatternInclude>() { new TMDefinition.TextMatePatternInclude() { Include = $"#{rootDefinition.Name.ToLower()}" } },
            Repository = GetTextMateRepository(this, messageContext)
        };

        JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new KeyValuePairArrayConverter() }
        };

        return JsonSerializer.Serialize(document, serializerOptions);
    }

    public String ToLanguageConfig()
    {
        String GetTextValueOfDefinition(Definition definition)
        {
            AbstractDefinitionElement definitionElement = definition.DefinitionElement;
            while (definitionElement is not TextTerminal)
            {
                if (definitionElement is AbstractMarker marker)
                    definitionElement = marker.Element ?? throw new Exception("Element required!");
                else
                    throw new Exception("Quoted definitions can only include literals!");
            }
            return (definitionElement as TextTerminal)!.AsText().ReplaceAll(new (String Search, String Replace)[] { ("\\'", "'"), ("\\\"", "\""), ("\\\\", "\\") });
        }
        List<KeyValuePair<String, String>> brackets = new List<KeyValuePair<String, String>>();
        List<KeyValuePair<String, String>> autoClosingPairs = new List<KeyValuePair<String, String>>();
        List<KeyValuePair<String, String>> surroundingPairs = new List<KeyValuePair<String, String>>();
        foreach (Definition definition in Definitions)
        {
            if (definition.KeyValuePairs.ContainsKey("quote"))
            {
                String quoteValue = GetTextValueOfDefinition(definition);
                autoClosingPairs.Add(new KeyValuePair<String, String>(quoteValue, quoteValue));
                surroundingPairs.Add(new KeyValuePair<String, String>(quoteValue, quoteValue));
            }
            if (definition.KeyValuePairs.ContainsKey("bracketopen"))
            {
                String bracketIdentifier = definition.KeyValuePairs["bracketopen"];
                String? closeBracket = null;
                foreach (Definition correspondingDefinition in Definitions)
                {
                    if ((correspondingDefinition != definition) && correspondingDefinition.KeyValuePairs.ContainsKey("bracketclose") && (correspondingDefinition.KeyValuePairs["bracketclose"] == bracketIdentifier))
                    {
                        closeBracket = GetTextValueOfDefinition(correspondingDefinition);
                        break;
                    }
                }
                if (!String.IsNullOrEmpty(closeBracket))
                {
                    String openBracket = GetTextValueOfDefinition(definition);
                    brackets.Add(new KeyValuePair<String, String>(openBracket, closeBracket!));
                    autoClosingPairs.Add(new KeyValuePair<String, String>(openBracket, closeBracket!));
                    surroundingPairs.Add(new KeyValuePair<String, String>(openBracket, closeBracket!));
                }
                else
                    throw new Exception($"A closing bracket for \"bracketopen: {bracketIdentifier}\" is required!");
            }
        }
        String? lineComment = TryGetOptionValue("linecomment");
        KeyValuePair<String, String>? blockComment = null;

        VSCodeLanguageConfDocument document = new VSCodeLanguageConfDocument
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

        JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new KeyValuePairArrayConverter() }
        };

        return JsonSerializer.Serialize(document, serializerOptions);
    }

    public String ToPackage()
    {
        String languageDisplayName = GetOptionValue("displayname");
        String languageName = (TryGetOptionValue("name") ?? languageDisplayName).ToLower().Replace(" ", "");

        VSCodePackageDocument document = new VSCodePackageDocument
        {
            Name = languageName,
            DisplayName = languageDisplayName,
            Description = TryGetOptionValue("description"),
            Version = GetOptionValue("version"),
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
                            ScopeName = TryGetOptionValue("scopename") ?? $"source.{languageName}",
                            Path = $"./syntaxes/{languageName}.tmLanguage.json"
                        }
                    )
                }
        };

        JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(document, serializerOptions);
    }

    private IReadOnlyDictionary<String, TMDefinition.TextMateRepositoryEntry> GetTextMateRepository(Grammar grammar, MessageContext messageContext)
    {
        var result = new Dictionary<String, TMDefinition.TextMateRepositoryEntry>(StringComparer.OrdinalIgnoreCase);
        List<TMDefinition> tmdefinitions = TMDefinitions.ToList();
        foreach (var definition in Definitions)
            if (definition.KeyValuePairs.ContainsKey(TextMatePropertyPattern))
            {
                foreach (TMDefinition tmdefinition in tmdefinitions)
                    if (tmdefinition.Name.Equals(definition.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        (UInt32 row, UInt32 column) = MessageContext!.CalculateLocation(definition.Node.Position);
                        throw new GrammarException($"TextMate definition '{definition.Name}' already exists!", row, column);
                    }
                String? scopeName = definition.KeyValuePairs[TextMatePropertyPattern];
                scopeName = String.IsNullOrEmpty(scopeName) ? null : scopeName;
                TMSequence sequence = new TMSequence(new List<AbstractDefinitionElement>() { definition.DefinitionElement }, messageContext, definition.Node);
                tmdefinitions.Add(new TMDefinition(definition.Name, scopeName, sequence, null, null, messageContext, definition.Node));
            }
        foreach (TMDefinition tmdefinition in tmdefinitions)
            result[tmdefinition.Name.ToLower()] = tmdefinition.GetRepositoryEntry(grammar);
        return result;
    }

    private String[] GetFileTypes()
    {
        String rawValue = GetOptionValue("filetype");
        String[] parts = rawValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0).ToArray(); ;
        return parts.Length == 0 ? new[] { rawValue.Trim() } : parts;
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

    private sealed class VSCodePackageDocument
    {
        [JsonPropertyName("name")]
        public String Name { get; set; } = String.Empty;

        [JsonPropertyName("displayName")]
        public String DisplayName { get; set; } = String.Empty;

        [JsonPropertyName("description")]
        public String? Description { get; set; } = String.Empty;

        [JsonPropertyName("version")]
        public String Version { get; set; } = String.Empty;

        [JsonPropertyName("engines")]
        public IReadOnlyDictionary<String, String> Engines { get; set; } = ImmutableDictionary.Create<String, String>().Add("vscode", "^1.106.1");

        [JsonPropertyName("categories")]
        public IReadOnlyList<String> Categories { get; set; } = ImmutableArray.Create<String>().Add("Programming Languages");

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

    public override String ToParserCode(Grammar grammar)
    {
        return
            $$"""
            #nullable enable

            using System.Text;
            using System.Text.RegularExpressions;

            namespace {{GetOptionValue("namespace")}}
            {
            {{Indent(GetIVisitorCode())}}

            {{Indent(GetParseResultCode())}}

            {{Indent(GetGlobalClassesCode())}}

                public class {{GetOptionValue("class")}}
                {
            {{Indent(Indent(GetParseCode()))}}

            {{Indent(Indent(GetBasicCode()))}}

            {{Indent(Indent(GetCheckDefinitionCode()))}}
                }
            }
            """.TrimLineEndWhitespace();
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
        foreach (Definition element in Definitions)
            if (element.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return element;
        return null;
    }

    public TMDefinition? FindTMDefinitionByName(String name)
    {
        foreach (TMDefinition element in TMDefinitions)
            if (element.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return element;
        return null;
    }

    public void CheckDuplicatedDefinitions(List<Definition> definitions)
    {
        HashSet<String> existingDefinitions = new HashSet<String>(StringComparer.InvariantCultureIgnoreCase);
        foreach (Definition definition in definitions)
            if (!existingDefinitions.Add(definition.Name))
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
        String? rootName = GetOptionValue("root");
        if (String.IsNullOrWhiteSpace(rootName))
            throw GetException("Grammar must have root option!");
        Definition? definition = FindDefinitionByName(rootName);
        if (definition is null)
            throw GetException($"Can not find root definition '{rootName}'!");
        return definition;
    }

    public TMDefinition GetTMRootDefinition()
    {
        String? rootName = GetOptionValue("root");
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
        foreach (ValuePair value in Options)
        {
            if (value.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                return value.Value;
        }
        return null;
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

    protected String GetParseResultCode()
    {
        String GetEventName(Definition definition) => $"Process{definition.Name.Humanize().Dehumanize()}Node";
        List<Definition> usedDefinitions = GetRelevantGrammarDefinitions();
        String visitorCalls = "";
        foreach (Definition definition in usedDefinitions)
            visitorCalls += $"case {GetElementIdOf(definition)}: return visitor.{GetEventName(definition)}(context, node, messages);\n";

        String result =
            $$"""
            public class MessageContext
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

            public class ParseResult
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
                            if (visitor is INodeVisitor)
                                DoVisit(context, (visitor as INodeVisitor)!, RootNode!, visitMessages);
                            return visitor.GetResult(context, true, visitMessages);
                        }
                        catch (GrammarException ex)
                        {
                            visitMessages.Add(new ParserMessage(ex.Message, ParserMessage.MessageType.Error, (ex.Row, ex.Column)));
                        }
                    }
                    return new EmptyResult(false, visitMessages);
                }

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
            {{Indent(Indent(Indent(visitorCalls)))}}
                    }
                    return ProcessNodeResult.Success;
                }
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

    protected String GetIVisitorCode()
    {
        String GetEventName(Definition definition) => $"Process{definition.Name.Humanize().Dehumanize()}Node";
        List<Definition> usedDefinitions = GetRelevantGrammarDefinitions();
        String visitorEvents = "";
        foreach (Definition definition in usedDefinitions)
            visitorEvents += $"ProcessNodeResult {GetEventName(definition)}(Object context, ASTNode node, IList<ParserMessage> messages);\n";
        String result =
            $$"""
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

            """;
        return result;
    }

    protected String GetBasicCode()
    {
        String result =
            $$"""
            private class ParserState
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

            public class ASTNode
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

            public class ParserMessage
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

}
