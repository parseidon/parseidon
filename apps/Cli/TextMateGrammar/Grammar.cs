using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Parseidon.Parser;
using Parseidon.Cli.TextMateGrammar.Block;
using System.Collections.Immutable;
using Parseidon.Cli.TextMateGrammar.Terminals;
using Parseidon.Cli.TextMateGrammar.Operators;
using System.Text.Encodings.Web;
using Parseidon.Helper;

namespace Parseidon.Cli.TextMateGrammar;

public class Grammar : AbstractNamedElement
{
    public Grammar(List<SimpleRule> rules, List<ValuePair> options, MessageContext messageContext, ASTNode node) : base("", messageContext, node)
    {
        Rules = rules;
        Options = options;
        CheckDuplicatedRules(Rules);
        Rules.ForEach((element) => element.Parent = this);
    }

    public List<SimpleRule> Rules { get; }
    public List<ValuePair> Options { get; }

    public override String ToString(Grammar grammar)
    {
        IReadOnlyList<SimpleRule> requiredRules = GetRequiredRules();
        IReadOnlyList<SimpleRule> patternRules = requiredRules.Where(rule => rule.HasTextMateName && !rule.IsIgnored).ToList();
        if (patternRules.Count == 0)
            throw GetException("No exportable rules found for TextMate generation.");

        TextMateRegexBuilder regexBuilder = new TextMateRegexBuilder(this);

        Dictionary<String, TextMateRepositoryEntry> repository = new Dictionary<String, TextMateRepositoryEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (SimpleRule rule in patternRules)
            repository[rule.GetRepositoryKey()] = BuildRepositoryEntry(rule, regexBuilder);

        List<TextMatePatternInclude> patterns = patternRules
            .Select(rule => new TextMatePatternInclude { Include = $"#{rule.GetRepositoryKey()}" })
            .ToList();

        TextMateGrammarDocument document = new TextMateGrammarDocument
        {
            DisplayName = GetOptionValue("displayname"),
            ScopeName = GetOptionValue("scopename"),
            FileTypes = GetFileTypes(),
            Patterns = patterns,
            Repository = repository
        };

        JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(document, serializerOptions);
    }

    public override String ToString() => ToString(this);

    public SimpleRule? FindRuleByName(String name)
    {
        List<SimpleRule> rules = new List<SimpleRule>();
        foreach (SimpleRule element in Rules)
            if (element.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return element;
        return null;
    }

    public void CheckDuplicatedRules(List<SimpleRule> rules)
    {
        HashSet<String> existingRules = new HashSet<String>(StringComparer.InvariantCultureIgnoreCase);
        foreach (SimpleRule rule in rules)
            if (!existingRules.Add(rule.Name))
                throw rule.GetException($"Rule '{rule.Name}' already exists!");
    }

    public Int32 GetElementIdOf(AbstractNamedElement element)
    {
        if ((element is SimpleRule) && (Rules.IndexOf((SimpleRule)element) >= 0))
            return Rules.IndexOf((SimpleRule)element);
        throw GetException($"Can not find identifier '{element.Name}'!");
    }

    private String GetOptionValue(String key)
    {
        foreach (ValuePair value in Options)
        {
            if (value.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                return value.Value;
        }
        throw GetException($"Can not find option '{key}'!");
    }

    private String? TryGetOptionValue(String key)
    {
        foreach (ValuePair value in Options)
        {
            if (value.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                return value.Value;
        }
        return null;
    }

    private IReadOnlyList<SimpleRule> GetRequiredRules()
    {
        Dictionary<String, SimpleRule> required = new Dictionary<String, SimpleRule>(StringComparer.OrdinalIgnoreCase);
        Queue<SimpleRule> queue = new Queue<SimpleRule>();

        foreach (SimpleRule rule in Rules)
        {
            if (rule.HasTextMateName && required.TryAdd(rule.Name, rule))
                queue.Enqueue(rule);
        }

        if (required.Count == 0)
            throw GetException("No rules define the 'tmname' option required for TextMate generation.");

        while (queue.Count > 0)
        {
            SimpleRule current = queue.Dequeue();
            foreach (String referencedName in current.GetReferencedRuleNames())
            {
                if (required.ContainsKey(referencedName))
                    continue;
                SimpleRule? referencedRule = FindRuleByName(referencedName);
                if (referencedRule is null)
                    throw current.GetException($"Rule '{current.Name}' references unknown rule '{referencedName}'.");
                required.Add(referencedRule.Name, referencedRule);
                queue.Enqueue(referencedRule);
            }
        }

        return Rules.Where(rule => required.ContainsKey(rule.Name)).ToList();
    }

    private TextMateRepositoryEntry BuildRepositoryEntry(SimpleRule rule, TextMateRegexBuilder regexBuilder)
    {
        String match = NormalizeMatch(regexBuilder.Build(rule));
        if (String.IsNullOrWhiteSpace(match))
            throw rule.GetException($"Rule '{rule.Name}' produced an empty regular expression for TextMate generation.");

        if (TryCreateBeginEndEntry(rule, match, out TextMateRepositoryEntry? beginEndEntry))
            return beginEndEntry;

        return new TextMateRepositoryEntry
        {
            Name = rule.TryGetTextMateName(out String? scope) ? scope?.Trim() : null,
            Match = match
        };
    }

    private static String NormalizeMatch(String match)
    {
        if (String.IsNullOrEmpty(match))
            return match;
        return match
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    private Boolean TryCreateBeginEndEntry(SimpleRule rule, String matchExpression, [NotNullWhen(true)] out TextMateRepositoryEntry? entry)
    {
        entry = null;
        if (String.IsNullOrEmpty(matchExpression))
            return false;

        if (!TryGetDelimiter(matchExpression, out Char delimiter))
            return false;

        String delimiterPattern = Regex.Escape(delimiter.ToString());
        String scope = rule.TryGetTextMateName(out String? value) ? (value ?? rule.Name).Trim() : rule.Name;

        entry = new TextMateRepositoryEntry
        {
            Name = scope,
            Begin = delimiterPattern,
            End = $"(?<!\\\\){delimiterPattern}",
            BeginCaptures = CreateDelimiterCapture(scope, "begin"),
            EndCaptures = CreateDelimiterCapture(scope, "end")
        };
        return true;
    }

    private static Boolean TryGetDelimiter(String expression, out Char delimiter)
    {
        delimiter = default;
        if (String.IsNullOrEmpty(expression))
            return false;

        Char[] candidates = new[] { '"', '\'' };
        foreach (Char candidate in candidates)
        {
            String raw = candidate.ToString();
            String escaped = $"\\{candidate}";
            Boolean hasStart = expression.StartsWith(escaped, StringComparison.Ordinal) || expression.StartsWith(raw, StringComparison.Ordinal);
            Boolean hasEnd = expression.EndsWith(escaped, StringComparison.Ordinal) || expression.EndsWith(raw, StringComparison.Ordinal);
            if (hasStart && hasEnd)
            {
                delimiter = candidate;
                return true;
            }
        }
        return false;
    }

    private static IReadOnlyDictionary<String, TextMateCapture> CreateDelimiterCapture(String scope, String suffix)
    {
        return new Dictionary<String, TextMateCapture>
        {
            ["0"] = new TextMateCapture { Name = $"{scope}.delimiter.{suffix}" }
        };
    }

    private String[] GetFileTypes()
    {
        String rawValue = GetOptionValue("filetype");
        String[] parts = rawValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? new[] { rawValue.Trim() } : parts;
    }

    public String ToLanguageConfigJson()
    {
        String GetTextValueOfRule(SimpleRule rule)
        {
            AbstractDefinitionElement definition = rule.Definition;
            while (definition is not TextTerminal)
            {
                if (definition is AbstractMarker marker)
                    definition = marker.Element ?? throw new Exception("Element required!");
                else
                    throw new Exception("Quoted rules can only include literals!");
            }
            return (definition as TextTerminal)!.AsText().ReplaceAll("\\'", "'").ReplaceAll("\\\"", "\"").ReplaceAll("\\\\", "\\");
        }
        List<KeyValuePair<String, String>> brackets = new List<KeyValuePair<String, String>>();
        List<KeyValuePair<String, String>> autoClosingPairs = new List<KeyValuePair<String, String>>();
        List<KeyValuePair<String, String>> surroundingPairs = new List<KeyValuePair<String, String>>();
        foreach (SimpleRule rule in Rules)
        {
            if (rule.KeyValuePairs.ContainsKey("quote"))
            {
                String quoteValue = GetTextValueOfRule(rule);
                autoClosingPairs.Add(new KeyValuePair<String, String>(quoteValue, quoteValue));
                surroundingPairs.Add(new KeyValuePair<String, String>(quoteValue, quoteValue));
            }
            if (rule.KeyValuePairs.ContainsKey("bracketopen"))
            {
                String bracketIdentifier = rule.KeyValuePairs["bracketopen"];
                String? closeBracket = null;
                foreach (SimpleRule correspondingRule in Rules)
                {
                    if ((correspondingRule != rule) && correspondingRule.KeyValuePairs.ContainsKey("bracketclose") && (correspondingRule.KeyValuePairs["bracketclose"] == bracketIdentifier))
                    {
                        closeBracket = GetTextValueOfRule(correspondingRule);
                        break;
                    }
                }
                if (!String.IsNullOrEmpty(closeBracket))
                {
                    String openBracket = GetTextValueOfRule(rule);
                    brackets.Add(new KeyValuePair<String, String>(openBracket, closeBracket));
                    autoClosingPairs.Add(new KeyValuePair<String, String>(openBracket, closeBracket));
                    surroundingPairs.Add(new KeyValuePair<String, String>(openBracket, closeBracket));
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

    public String ToPackageJson()
    {
        String languageDisplayName = GetOptionValue("displayname");
        String languageName = (TryGetOptionValue("name") ?? languageDisplayName).ToLower().Replace(" ", "");

        VSCodePackageDocument document = new VSCodePackageDocument
        {
            Name = languageName,
            DisplayName = languageDisplayName,
            Description = GetOptionValue("description"),
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

    public String LanguageName { get; private set; } = String.Empty;

    private sealed class TextMateGrammarDocument
    {
        [JsonPropertyName("displayName")]
        public String DisplayName { get; init; } = String.Empty;

        [JsonPropertyName("scopeName")]
        public String ScopeName { get; init; } = String.Empty;

        [JsonPropertyName("fileTypes")]
        public IReadOnlyList<String> FileTypes { get; init; } = Array.Empty<String>();

        [JsonPropertyName("patterns")]
        public IReadOnlyList<TextMatePatternInclude> Patterns { get; init; } = Array.Empty<TextMatePatternInclude>();

        [JsonPropertyName("repository")]
        public IReadOnlyDictionary<String, TextMateRepositoryEntry> Repository { get; init; } = new Dictionary<String, TextMateRepositoryEntry>();
    }

    private sealed class TextMatePatternInclude
    {
        [JsonPropertyName("include")]
        public String Include { get; init; } = String.Empty;
    }

    private sealed class TextMateRepositoryEntry
    {
        [JsonPropertyName("name")]
        public String? Name { get; set; }

        [JsonPropertyName("match")]
        public String? Match { get; set; }

        [JsonPropertyName("begin")]
        public String? Begin { get; set; }

        [JsonPropertyName("end")]
        public String? End { get; set; }

        [JsonPropertyName("captures")]
        public IReadOnlyDictionary<String, TextMateCapture>? Captures { get; set; }

        [JsonPropertyName("beginCaptures")]
        public IReadOnlyDictionary<String, TextMateCapture>? BeginCaptures { get; set; }

        [JsonPropertyName("endCaptures")]
        public IReadOnlyDictionary<String, TextMateCapture>? EndCaptures { get; set; }

        [JsonPropertyName("patterns")]
        public IReadOnlyList<TextMatePatternInclude>? Patterns { get; set; }
    }

    private sealed class TextMateCapture
    {
        [JsonPropertyName("name")]
        public String Name { get; init; } = String.Empty;
    }

    private sealed class VSCodePackageDocument
    {
        [JsonPropertyName("name")]
        public String Name { get; init; } = String.Empty;

        [JsonPropertyName("displayName")]
        public String DisplayName { get; init; } = String.Empty;

        [JsonPropertyName("description")]
        public String Description { get; init; } = String.Empty;

        [JsonPropertyName("version")]
        public String Version { get; init; } = String.Empty;

        [JsonPropertyName("engines")]
        public IReadOnlyDictionary<String, String> Engines { get; init; } = ImmutableDictionary.Create<String, String>().Add("vscode", "^1.106.1");

        [JsonPropertyName("categories")]
        public IReadOnlyList<String> Categories { get; init; } = ImmutableArray.Create<String>().Add("Programming Languages");

        [JsonPropertyName("contributes")]
        public VSCodePackageContributes Contributes { get; init; } = new VSCodePackageContributes();
    }

    private sealed class VSCodePackageContributes
    {
        [JsonPropertyName("languages")]
        public IReadOnlyList<VSCodePackageLanguage> Languages { get; init; } = Array.Empty<VSCodePackageLanguage>();

        [JsonPropertyName("grammars")]
        public IReadOnlyList<VSCodePackageGrammar> Grammars { get; init; } = Array.Empty<VSCodePackageGrammar>();
    }

    private sealed class VSCodePackageLanguage
    {
        [JsonPropertyName("id")]
        public String Id { get; init; } = String.Empty;

        [JsonPropertyName("aliases")]
        public IReadOnlyList<String> Aliases { get; init; } = Array.Empty<String>();

        [JsonPropertyName("extensions")]
        public IReadOnlyList<String> Extensions { get; init; } = Array.Empty<String>();

        [JsonPropertyName("configuration")]
        public String Configuration { get; init; } = "./language-configuration.json";
    }

    private sealed class VSCodePackageGrammar
    {
        [JsonPropertyName("language")]
        public String Language { get; init; } = String.Empty;

        [JsonPropertyName("scopeName")]
        public String ScopeName { get; init; } = String.Empty;

        [JsonPropertyName("path")]
        public String Path { get; init; } = String.Empty;
    }

    private sealed class VSCodeLanguageConfDocument
    {
        [JsonPropertyName("comments")]
        public VSCodeLanguageConfComments Comments { get; init; } = new VSCodeLanguageConfComments();

        [JsonPropertyName("brackets")]
        public IList<KeyValuePair<String, String>> Brackets { get; init; } = Array.Empty<KeyValuePair<String, String>>();

        [JsonPropertyName("autoClosingPairs")]
        public IList<KeyValuePair<String, String>> AutoClosingPairs { get; init; } = Array.Empty<KeyValuePair<String, String>>();

        [JsonPropertyName("surroundingPairs")]
        public IList<KeyValuePair<String, String>> SurroundingPairs { get; init; } = Array.Empty<KeyValuePair<String, String>>();
    }

    private sealed class VSCodeLanguageConfComments
    {
        [JsonPropertyName("lineComment")]
        public String? LineComment { get; init; }

        [JsonPropertyName("blockComment")]
        public KeyValuePair<String, String>? BlockComment { get; init; }
    }

    public class KeyValuePairArrayConverter : JsonConverter<KeyValuePair<String, String>>
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
}
