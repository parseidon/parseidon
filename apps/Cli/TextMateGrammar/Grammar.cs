using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Parseidon.Parser;
using Parseidon.Cli.TextMateGrammar.Block;

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
            DisplayName = GetOptionValue("tmdisplayname"),
            ScopeName = GetOptionValue("tmscopename"),
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
        String rawValue = GetOptionValue("tmfiletype");
        String[] parts = rawValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? new[] { rawValue.Trim() } : parts;
    }

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
}
