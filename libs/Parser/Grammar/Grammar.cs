using System.Text;
using Humanizer;
using Parseidon.Helper;
using Parseidon.Parser.Grammar.Block;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar;

public class Grammar : AbstractNamedElement
{
    private String _namespace;
    private String _rootRuleName;

    public Grammar(String nameSpace, String className, String rootRuleName, List<SimpleRule> rules, MessageContext messageContext, ASTNode node) : base(className, messageContext, node)
    {
        Rules = rules;
        Rules.ForEach((element) => element.Parent = this);
        _namespace = nameSpace;
        _rootRuleName = String.IsNullOrWhiteSpace(rootRuleName) ? "Grammar" : rootRuleName;
    }

    public List<SimpleRule> Rules { get; }

    public override String ToString(Grammar grammar)
    {
        return
            $$"""
            #nullable enable

            using System.Text;
            using System.Text.RegularExpressions;

            namespace {{_namespace}}
            {
            {{Indent(GetIVisitorCode())}}

            {{Indent(GetParseResultCode())}}

            {{Indent(GetGlobalClassesCode())}}

                public class {{Name}}
                {
            {{Indent(Indent(GetParseCode()))}}

            {{Indent(Indent(GetBasicCode()))}}

            {{Indent(Indent(GetCheckRuleCode()))}}
                }
            }
            """.TrimLineEndWhitespace();
    }

    public override String ToString() => ToString(this);

    public override bool MatchesVariableText()
    {
        Boolean result = false;
        foreach (SimpleRule rule in Rules)
            result = result || rule.MatchesVariableText();
        return result;
    }

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if (process(this))
            foreach (SimpleRule rule in Rules)
                rule.IterateElements(process);
    }

    public SimpleRule? FindRuleByName(String name)
    {
        List<SimpleRule> rules = new List<SimpleRule>();
        foreach (SimpleRule element in Rules)
            if (element.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                return element;
        return null;
    }

    public Int32 GetElementIdOf(AbstractNamedElement element)
    {
        if ((element is SimpleRule) && (Rules.IndexOf((SimpleRule)element) >= 0))
            return Rules.IndexOf((SimpleRule)element);
        throw GetException($"Can not find identifier '{element.Name}'!");
    }

    public SimpleRule GetRootRule()
    {
        String? axiomName = _rootRuleName;
        if (String.IsNullOrWhiteSpace(axiomName))
            throw GetException("Grammar must have axiom option!");
        SimpleRule? rule = FindRuleByName(axiomName);
        if (rule is null)
            throw GetException($"Can not find axiom option '{axiomName}'!");
        return rule;
    }

    private Boolean IterateUsedRules(AbstractGrammarElement element, List<SimpleRule> rules)
    {
        if ((element is SimpleRule rule) && (rules.IndexOf(rule) < 0))
            rules.Add(rule);
        else
        if ((element is ReferenceElement referenceElement) && (FindRuleByName(referenceElement.ReferenceName) is SimpleRule referencedRule) && (rules.IndexOf(referencedRule) < 0))
            referencedRule.IterateElements((element) => IterateUsedRules(element, rules));
        return true;
    }

    private List<SimpleRule> GetUsedRules()
    {
        List<SimpleRule> result = new List<SimpleRule>();
        SimpleRule rootRule = GetRootRule();
        result.Add(rootRule);
        rootRule.IterateElements((element) => IterateUsedRules(element, result));
        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return result;
    }

    private Boolean IterateRelevantGrammarRules(AbstractGrammarElement element, List<SimpleRule> rules, Boolean forceAdd)
    {
        if ((element is SimpleRule rule) && (rules.IndexOf(rule) < 0) && !(rule.Definition is DropMarker) && (rule.MatchesVariableText() || forceAdd))
            rules.Add(rule);
        else
        if ((element is ReferenceElement referenceElement) && (FindRuleByName(referenceElement.ReferenceName) is SimpleRule referencedRule) && (rules.IndexOf(referencedRule) < 0))
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
                referencedRule.IterateElements(
                    (element) => IterateRelevantGrammarRules(element, rules, hasOrParent)
                );
            }
        }
        Boolean result = !((element is SimpleRule rule1) && (rule1.Definition is IsTerminalMarker));
        return result;
    }

    private List<SimpleRule> GetRelevantGrammarRules()
    {
        List<SimpleRule> result = new List<SimpleRule>();
        SimpleRule rootRule = GetRootRule();
        result.Add(rootRule);
        rootRule.IterateElements((element) => IterateRelevantGrammarRules(element, result, false));
        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return result;
    }

    protected String GetCheckRuleCode()
    {
        StringBuilder builder = new StringBuilder();
        foreach (SimpleRule rule in GetUsedRules())
        {
            String ruleCode =
                $$"""
                private Boolean CheckRule_{{rule.Name}}(ASTNode parentNode, ParserState state)
                {
                    Int32 oldPosition = state.Position;
                    ASTNode actualNode = new ASTNode({{GetElementIdOf(rule)}}, "{{rule.Name}}", "", state.Position);
                    Boolean result = {{GetElementsCode(new List<AbstractNamedDefinitionElement>() { rule }, "Rule", null)}}
                    Int32 foundPosition = state.Position;
                    if (result && ((actualNode.Children.Count > 0) || (actualNode.Text != "")))
                        parentNode.AddChild(actualNode);
                    return result;
                }

                """;

            builder.AppendLine(ruleCode);
        }
        return builder.ToString();
    }

    protected String GetParseResultCode()
    {
        String GetEventName(SimpleRule rule) => $"Process{rule.Name.Humanize().Dehumanize()}Node";
        SimpleRule rootRule = GetRootRule();
        List<SimpleRule> usedRules = GetRelevantGrammarRules();
        String visitorCalls = "";
        foreach (SimpleRule rule in usedRules)
            visitorCalls += $"case {GetElementIdOf(rule)}: return visitor.{GetEventName(rule)}(context, node, messages);\n";

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
                            visitMessages.Add(new ParserMessage(ex.Message, ParserMessage.MessageType.Error, ex.Row, ex.Column));
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
        SimpleRule rootRule = GetRootRule();
        String result =
            $$"""
            public ParseResult Parse(String text)
            {
                ParserState state = new ParserState(text, new MessageContext(text));
                ASTNode actualNode = new ASTNode(-1, "ROOT", "", 0);
                Boolean successful = {{rootRule.GetReferenceCode(this)}};
                return new ParseResult(successful ? actualNode : null, state.MessageContext, state.Messages);
            }
            """;
        return result;
    }

    private String GetElementsCode(IEnumerable<AbstractNamedDefinitionElement> elements, String comment, AbstractNamedDefinitionElement? separatorTerminal)
    {
        String result = String.Join("", elements.Select(x => x.ToString(this))) + ";";
        if (result.IndexOf("\n") > 0)
            result = "\n" + result;
        return Indent(Indent(result));
    }

    protected String GetIVisitorCode()
    {
        String GetEventName(SimpleRule rule) => $"Process{rule.Name.Humanize().Dehumanize()}Node";
        List<SimpleRule> usedRules = GetRelevantGrammarRules();
        String visitorEvents = "";
        foreach (SimpleRule rule in usedRules)
            visitorEvents += $"ProcessNodeResult {GetEventName(rule)}(Object context, ASTNode node, IList<ParserMessage> messages);\n";
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

                private Int32 _errorSuppressionDepth;
                private Int32 _lastErrorPosition = -1;
                private String? _lastExpected;
                private String? _lastActual;
                private readonly List<String> _terminalNames = new List<String>();

                internal String Text { get; }
                internal Int32 Position { get; set; } = 0;
                internal Boolean Eof => !(Position < Text.Length);
                internal List<ParserMessage> Messages { get; } = new List<ParserMessage>();
                internal Boolean AreErrorsSuppressed => _errorSuppressionDepth > 0;
                internal MessageContext MessageContext { get; }

                private String? GetCurrentTerminalName()
                {
                    return _terminalNames.Count > 0 ? _terminalNames[_terminalNames.Count - 1] : null;
                }

                public TerminalScope EnterTerminal(String? ruleName)
                {
                    Boolean pushed = false;
                    if (!String.IsNullOrWhiteSpace(ruleName))
                    {
                        _terminalNames.Add(ruleName!);
                        pushed = true;
                    }
                    return new TerminalScope(this, pushed);
                }

                private void LeaveTerminal(Boolean shouldPop)
                {
                    if (shouldPop && (_terminalNames.Count > 0))
                        _terminalNames.RemoveAt(_terminalNames.Count - 1);
                }

                public ErrorSuppressionScope SuppressErrors()
                {
                    BeginErrorSuppression();
                    return new ErrorSuppressionScope(this);
                }

                public void BeginErrorSuppression() => _errorSuppressionDepth++;

                public void EndErrorSuppression()
                {
                    if (_errorSuppressionDepth > 0)
                        _errorSuppressionDepth--;
                }

                public void ReportFailure(String expected, Int32 position, String? actual)
                {
                    if (AreErrorsSuppressed)
                        return;

                    String? terminalOverride = GetCurrentTerminalName();
                    if (!String.IsNullOrWhiteSpace(terminalOverride))
                        expected = terminalOverride!;

                    if ((_lastErrorPosition == position) &&
                        String.Equals(_lastExpected, expected, StringComparison.Ordinal) &&
                        String.Equals(_lastActual, actual, StringComparison.Ordinal))
                    {
                        return;
                    }

                    _lastErrorPosition = position;
                    _lastExpected = expected;
                    _lastActual = actual;

                    (UInt32 Row, UInt32 Column) location = MessageContext.CalculateLocation(position);
                    String message = actual is null
                        ? $"Expected {expected}."
                        : $"Expected {expected}, but {actual}.";
                    Messages.Add(new ParserMessage(message, ParserMessage.MessageType.Error, location.Row, location.Column));
                }

                public readonly struct ErrorSuppressionScope : IDisposable
                {
                    private readonly ParserState _state;

                    internal ErrorSuppressionScope(ParserState state)
                    {
                        _state = state;
                    }

                    public void Dispose()
                    {
                        _state.EndErrorSuppression();
                    }
                }

                public readonly struct TerminalScope : IDisposable
                {
                    private readonly ParserState _state;
                    private readonly Boolean _shouldPop;

                    internal TerminalScope(ParserState state, Boolean shouldPop)
                    {
                        _state = state;
                        _shouldPop = shouldPop;
                    }

                    public void Dispose()
                    {
                        _state.LeaveTerminal(_shouldPop);
                    }
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

            private static String DescribeCharacter(Char value) => $"'{EscapeCharacter(value)}'";

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

            private Boolean CheckRegEx(ASTNode parentNode, ParserState state, String regEx)
            {
                Int32 oldPosition = state.Position;
                if ((state.Position < state.Text.Length) && Regex.Match(state.Text[state.Position].ToString(), regEx).Success)
                {
                    state.Position++;
                    parentNode.AddChild(new ASTNode(-1, "REGEX", state.Text.Substring(oldPosition, state.Position - oldPosition), state.Position));
                    return true;
                }

                Int32 failurePosition = state.Position < state.Text.Length ? state.Position : state.Text.Length;
                state.Position = oldPosition;
                String actual = failurePosition < state.Text.Length
                    ? $"found {DescribeCharacter(state.Text[failurePosition])}"
                    : "found end of input";
                state.ReportFailure($"input matching regex {DescribePattern(regEx)}", oldPosition, actual);
                return false;
            }

            private Boolean CheckText(ASTNode parentNode, ParserState state, String text)
            {
                Int32 oldPosition = state.Position;
                Int32 position = 0;
                while (position < text.Length)
                {
                    if (state.Eof || (state.Text[state.Position] != text[position]))
                    {
                        Int32 failurePosition = state.Position < state.Text.Length ? state.Position : state.Text.Length;
                        state.Position = oldPosition;
                        String actual = failurePosition < state.Text.Length
                            ? $"found {DescribeLiteral(state.Text.Substring(oldPosition, failurePosition - oldPosition + 1))}"
                            : "found end of input";
                        state.ReportFailure($"text {DescribeLiteral(text)}", oldPosition, actual);
                        return false;
                    }
                    position++;
                    state.Position++;
                }
                parentNode.AddChild(new ASTNode(-1, "TEXT", state.Text.Substring(oldPosition, state.Position - oldPosition), state.Position));
                return true;
            }

            private Boolean CheckAnd(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> leftCheck, Func<ASTNode, Boolean> rightCheck)
            {
                Int32 oldPosition = state.Position;
                ASTNode tempNode = new ASTNode(parentNode.TokenId, "AND", parentNode.Text, state.Position);
                tempNode.Position = parentNode.Position;
                if (leftCheck(tempNode))
                {
                    if (rightCheck(tempNode))
                    {
                        parentNode.AddChild(tempNode);
                        parentNode.AssignFrom(tempNode);
                        return true;
                    }
                }
                state.Position = oldPosition;
                return false;
            }

            private Boolean CheckOr(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> leftCheck, Func<ASTNode, Boolean> rightCheck)
            {
                Int32 oldPosition = state.Position;
                using (state.SuppressErrors())
                {
                    if (leftCheck(parentNode))
                        return true;
                }
                state.Position = oldPosition;
                return rightCheck(parentNode);
            }

            private Boolean Drop(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                ASTNode tempNode = new ASTNode(-1, "", "", state.Position);
                return check(tempNode);
            }

            private Boolean CheckOneOrMore(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                if (!check(parentNode))
                    return false;

                oldPosition = state.Position;
                while (!state.Eof)
                {
                    Int32 snapshot = state.Position;
                    using (state.SuppressErrors())
                    {
                        if (!check(parentNode))
                        {
                            state.Position = snapshot;
                            break;
                        }
                    }
                    if (state.Position == snapshot)
                        break;
                    oldPosition = state.Position;
                }
                state.Position = oldPosition;
                return true;
            }

            private Boolean CheckZeroOrMore(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 lastSuccessfulPosition = state.Position;
                while ((!state.Eof))
                {
                    Int32 snapshot = state.Position;
                    using (state.SuppressErrors())
                    {
                        if (!check(parentNode))
                        {
                            state.Position = snapshot;
                            break;
                        }
                    }
                    if (state.Position == snapshot)
                        break;
                    lastSuccessfulPosition = state.Position;
                }
                state.Position = lastSuccessfulPosition;
                return true;
            }

            private Boolean CheckDifference(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> leftCheck, Func<ASTNode, Boolean> rightCheck)
            {
                Int32 oldPosition = state.Position;
                if (leftCheck(parentNode))
                {
                    state.Position = oldPosition;
                    using (state.SuppressErrors())
                    {
                        if (rightCheck(parentNode))
                            return true;
                    }
                }
                state.Position = oldPosition;
                return false;
            }

            private Boolean CheckRange(ASTNode parentNode, ParserState state, Int32 minCount, Int32 maxCount, Func<ASTNode, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                Int32 count = 0;

                while (count < minCount)
                {
                    if (!check(parentNode))
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
                    using (state.SuppressErrors())
                    {
                        if (!check(parentNode))
                        {
                            state.Position = snapshot;
                            break;
                        }
                    }
                    if (state.Position == snapshot)
                        break;
                    count++;
                    oldPosition = state.Position;
                }

                state.Position = oldPosition;
                return true;
            }

            private Boolean MakeTerminal(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                using (state.EnterTerminal(parentNode.Name))
                {
                    ASTNode tempNode = new ASTNode(-1, "", "", state.Position);
                    Boolean result = check(tempNode);
                    if (result)
                    {
                        tempNode.Text = tempNode.GetText();
                        tempNode.ClearChildren();
                        parentNode.Text = tempNode.GetText();
                    }
                    return result;
                }
            }

            private Boolean PromoteAction(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 childCount = parentNode.Children.Count;
                Boolean result = check(parentNode);
                if (result && (childCount < parentNode.Children.Count))
                {
                    ASTNode newNode = parentNode.Children.Last();
                    parentNode.AssignFrom(newNode);
                }
                return result;
            }

            private Boolean AddVirtualNode(ASTNode parentNode, ParserState state, Int32 tokenId, String text)
            {
                parentNode.AddChild(new ASTNode(tokenId, "VIRTUAL", text, state.Position));
                return true;
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

                public ParserMessage(String message, MessageType type, UInt32 row, UInt32 column)
                {
                    Message = message;
                    Row = row;
                    Column = column;
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
