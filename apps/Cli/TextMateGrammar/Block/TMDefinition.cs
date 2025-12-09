using Parseidon.Parser;

using Parseidon.Cli.TextMateGrammar.Operators;
using System.Text.Json.Serialization;
using System.IO.Pipelines;
using Parseidon.Cli.TextMateGrammar.Terminals;

namespace Parseidon.Cli.TextMateGrammar.Block;

public class TMDefinition : AbstractNamedElement
{
    public TMDefinition(String name, String? scopeName, TMSequence beginSequence, TMIncludes? includes, TMSequence? endSequence, MessageContext messageContext, ASTNode node) : base(name, messageContext, node)
    {
        ScopeName = scopeName;
        BeginSequence = beginSequence;
        Includes = includes;
        if (Includes is not null)
            Includes.Parent = this;
        EndSequence = endSequence;
        if (EndSequence is not null)
            EndSequence.Parent = this;
    }

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
        {
            BeginSequence.IterateElements(process);
            if (EndSequence is not null)
                EndSequence.IterateElements(process);
        }
    }

    public String? ScopeName { get; }
    public TMSequence BeginSequence { get; }
    public TMSequence? EndSequence { get; }
    public TMIncludes? Includes { get; }

    internal TextMateRepositoryEntry GetRepositoryEntry(Grammar grammar)
    {
        Dictionary<String, TextMateCapture> GetCaptures(AbstractDefinitionElement.RegExResult regEx)
        {
            Dictionary<String, TextMateCapture> result = new Dictionary<string, TextMateCapture>();
            Int32 index = 1;
            foreach (var capture in regEx.Captures)
            {
                result[index.ToString()] = new TextMateCapture() { Name = capture };
                index++;
            }
            return result;
        }

        TextMateRepositoryEntry result = new TextMateRepositoryEntry();
        result.Name = ScopeName;
        if ((BeginSequence is null) && (EndSequence is null))
        {
            if ((Includes is not null) && (Includes.Includes.Count > 0))
            {
                var patterns = new List<TextMatePatternInclude>();
                foreach (var include in Includes.Includes)
                    patterns.Add(new TextMatePatternInclude() { Include = $"#{include.ReferenceName.ToLower()}" });
                result.Patterns = patterns;
            }
        }
        else if (EndSequence is null)
        {
            var regEx = BeginSequence.GetRegEx(grammar);
            result.Match = regEx.RegEx;
            if (regEx.Captures.Count() > 0)
                result.Captures = GetCaptures(regEx);
        }
        else
        {
            var regEx = BeginSequence.GetRegEx(grammar);
            result.Begin = regEx.RegEx;
            if (regEx.Captures.Count() > 0)
                result.BeginCaptures = GetCaptures(regEx);
            if ((Includes is not null) && (Includes.Includes.Count > 0))
            {
                var patterns = new List<TextMatePatternInclude>();
                foreach (var include in Includes.Includes)
                    patterns.Add(new TextMatePatternInclude() { Include = $"#{include.ReferenceName.ToLower()}" });
                result.Patterns = patterns;
            }
            regEx = EndSequence.GetRegEx(grammar);
            result.End = regEx.RegEx;
            if (regEx.Captures.Count() > 0)
                result.EndCaptures = GetCaptures(regEx);
        }
        return result;
    }

    internal sealed class TextMateRepositoryEntry
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

    internal sealed class TextMateCapture
    {
        [JsonPropertyName("name")]
        public String Name { get; init; } = String.Empty;
    }

    internal sealed class TextMatePatternInclude
    {
        [JsonPropertyName("include")]
        public String Include { get; init; } = String.Empty;
    }
}
