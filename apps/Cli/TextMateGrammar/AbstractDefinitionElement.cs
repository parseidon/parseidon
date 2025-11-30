using Parseidon.Cli.TextMateGrammar.Block;
using Parseidon.Parser;


namespace Parseidon.Cli.TextMateGrammar;

public abstract class AbstractDefinitionElement : AbstractGrammarElement
{
    public AbstractDefinitionElement(MessageContext messageContext, ASTNode node) : base(messageContext, node) { }

    public sealed record Pattern(String? Name, String? Include, String? Match, String? Begin, String? End, IReadOnlyList<Capture>? Captures, IReadOnlyList<Pattern>? Patterns);
    public sealed record Capture(UInt32? Id, String Name);

    public SimpleRule GetRule()
    {
        AbstractGrammarElement? parent = Parent;
        while ((parent is not null) && (parent is not SimpleRule))
            parent = parent.Parent;
        if (parent is null)
            throw GetException("Can not find parent!");
        return (parent as SimpleRule)!;
    }

    public abstract class RegExResult
    {
        public abstract String GetBeginRegEx();
        public abstract String GetEndRegEx();
    }

    public class RegExBeginEndResult : RegExResult
    {
        public RegExBeginEndResult(String begin, String end, IReadOnlyDictionary<Int32, String>? beginCaptures, Int32 beginBracketCount, IReadOnlyDictionary<Int32, String>? endCaptures, Int32 endBracketCount, IReadOnlyList<String>? includes)
        {
            Begin = begin;
            BeginCaptures = beginCaptures ?? new Dictionary<Int32, String>();
            BeginBracketCount = beginBracketCount;
            if (BeginBracketCount < BeginCaptures.Count)
                throw new Exception("Bracket Count must have at least the number of given captures!");
            End = end;
            EndCaptures = endCaptures ?? new Dictionary<Int32, String>();
            EndBracketCount = endBracketCount;
            if (EndBracketCount < EndCaptures.Count)
                throw new Exception("Bracket Count must have at least the number of given captures!");
            Includes = includes ?? new List<String>();
        }

        public String Begin { get; }
        public IReadOnlyDictionary<Int32, String> BeginCaptures { get; }
        public Int32 BeginBracketCount { get; }
        public String End { get; }
        public IReadOnlyDictionary<Int32, String> EndCaptures { get; }
        public Int32 EndBracketCount { get; }
        public IReadOnlyList<String> Includes { get; }

        public override string ToString()
        {
            String GetCaptureString(String name, IReadOnlyDictionary<Int32, String> captures)
            {
                String result = "";
                foreach (KeyValuePair<Int32, String> valuePair in captures)
                {
                    result = $"{result}    \"{valuePair.Key}\": {{ \"name\": \"{valuePair.Value}\" }},\n";
                }
                if (result.Length > 1)
                    result = $",\n\"{name}\": {{\n{result.Substring(0, result.Length - 2)}}}";
                return result;
            }
            String GetIncludeString(IReadOnlyList<String> includes)
            {
                String result = "";
                foreach (String value in includes)
                {
                    result = $"{result}    {{\"include\": \"#{value}\"}},\n";
                }
                if (result.Length > 1)
                    result = $",\n\"patterns\": [\n{result.Substring(0, result.Length - 2)}]";
                return result;
            }
            return $$"""
            {
                "Begin": "{{Begin}}",
                "End": "{{End}}"{{GetCaptureString("beginCaptures", BeginCaptures)}}{{GetCaptureString("endCaptures", EndCaptures)}}{{GetIncludeString(Includes)}}
            },
            """;
        }
        public override String GetBeginRegEx() => Begin;
        public override String GetEndRegEx() => End;

    }

    public class RegExMatchResult : RegExResult
    {
        public RegExMatchResult(String match, IReadOnlyDictionary<Int32, String>? captures, Int32 bracketCount)
        {
            Match = match;
            Captures = captures ?? new Dictionary<Int32, String>();
            BracketCount = bracketCount;
            if (BracketCount < Captures.Count)
                throw new Exception("Bracket Count must have at least the number of given captures!");
        }

        public IReadOnlyDictionary<Int32, String> Captures { get; }
        public String Match { get; }
        public Int32 BracketCount { get; }

        public override string ToString()
        {
            String captureString = "";
            foreach (KeyValuePair<Int32, String> valuePair in Captures)
            {
                captureString = $"{captureString}    \"{valuePair.Key}\": {{ \"name\": \"{valuePair.Value}\" }},\n";
            }
            if (captureString.Length > 1)
                captureString = $",\n\"captures\": {{\n{captureString.Substring(0, captureString.Length - 2)}}}";
            return $$"""
            {
                "match": "{{Match}}"{{captureString}}
            },
            """;
        }

        public override String GetBeginRegEx() => Match;
        public override String GetEndRegEx() => Match;

    }

    public abstract RegExResult GetRegExChain(Grammar grammar, RegExResult before, RegExResult after);

    public virtual Boolean CanSubstitute(Grammar grammar) => false;

    public virtual AbstractDefinitionElement GetRegExDefinition(Grammar grammar) => this;

}
