using Parseidon.Helper;
using Parseidon.Parser.Grammar;
using Parseidon.Parser.Grammar.Blocks;
using Parseidon.Parser.Grammar.Operators;
using Parseidon.Parser.Grammar.Terminals;

namespace Parseidon.Parser;

public class ParseidonVisitor : INodeVisitor<ParseidonVisitor.CreateCodeVisitorContext>
{
    public interface IGetResults
    {
        Grammar.Grammar.CreateOutputResult GetParserCode(String? namespaceOverride = null, String? classOverride = null, Boolean? generateNodeVisitorOverride = null);
        Grammar.Grammar.CreateOutputResult GetTextMateGrammar();
        Grammar.Grammar.CreateOutputResult GetLanguageConfig();
        Grammar.Grammar.CreateOutputResult GetVSCodePackage(String? versionOverride, Func<String, String>? loadMergeJson, String? packageJsonOverridePath);
    }

    public class CreateCodeVisitorContext : StackVisitorContext<AbstractGrammarElement>
    {
        public CreateCodeVisitorContext(String text) : base(text) { }

        internal Grammar.Grammar? Grammar { get; set; }
    }

    private class CreateCodeVisitResult : IVisitResult, IGetResults
    {
        public CreateCodeVisitResult(Boolean successful, Func<Int32, (UInt32, UInt32)> calcLocation, IReadOnlyList<ParserMessage> messages, Grammar.Grammar grammar)
        {
            Successful = successful;
            CalcLocation = calcLocation;
            Messages = messages;
            _grammar = grammar;
        }

        private Grammar.Grammar _grammar;
        private Func<Int32, (UInt32, UInt32)> CalcLocation { get; }
        public Boolean Successful { get; }
        public IReadOnlyList<ParserMessage> Messages { get; }

        public Grammar.Grammar.CreateOutputResult GetParserCode(String? namespaceOverride = null, String? classOverride = null, Boolean? generateNodeVisitorOverride = null) => _grammar.ToParserCode(namespaceOverride, classOverride, generateNodeVisitorOverride);

        public Grammar.Grammar.CreateOutputResult GetTextMateGrammar() => _grammar.ToTextMateGrammar();

        public Grammar.Grammar.CreateOutputResult GetLanguageConfig() => _grammar.ToLanguageConfig();

        public Grammar.Grammar.CreateOutputResult GetVSCodePackage(String? versionOverride, Func<String, String>? loadMergeJson, String? packageJsonOverridePath) => _grammar.ToVSCodePackage(versionOverride, loadMergeJson, packageJsonOverridePath);
    }

    public ProcessNodeResult ProcessGrammarNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        IEnumerable<Definition> definitions = new List<Definition>();
        IEnumerable<TMDefinition> tmDefinitions = new List<TMDefinition>();
        List<Definition> tempDefinitions = new List<Definition>();
        List<TMDefinition> tempTMDefinitions = new List<TMDefinition>();
        do
        {
            tempDefinitions = context.PopList<Definition>();
            definitions = definitions.Concat(tempDefinitions);
            tempTMDefinitions = context.PopList<TMDefinition>();
            tmDefinitions = tmDefinitions.Concat(tempTMDefinitions);
        } while ((tempDefinitions.Count > 0) || (tempTMDefinitions.Count > 0));
        List<ValuePair> options = context.PopList<ValuePair>();
        context.Grammar = new Grammar.Grammar(definitions.ToList(), tmDefinitions.ToList(), options, context.CalcLocation, node);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessIsTerminalNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new IsTerminalMarker(node.Children.Count == 2, null, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessDropNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new DropMarker(null, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessDefinitionNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<ValuePair> valuePairs = context.PopList<ValuePair>();
        AbstractDefinitionElement definition = context.Pop<AbstractDefinitionElement>();
        ReferenceElement name = context.Pop<ReferenceElement>();
        List<AbstractMarker> markers = new List<AbstractMarker>();
        while (context.TryPop<AbstractMarker>() is AbstractMarker marker)
        {
            markers.Add(marker);
            marker.Element = definition;
            definition = marker;
        }
        AbstractGrammarElement newDefinition = new Definition(name.ReferenceName, definition, valuePairs, context.CalcLocation, node, markers);
        context.Push(newDefinition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessIdentifierNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new ReferenceElement(node.Text.Trim(), context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessCSharpIdentifierNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new ReferenceElement(node.Text.Trim(), context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessExpressionNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<AbstractDefinitionElement> elements = context.PopList<AbstractDefinitionElement>();
        if (elements.Count < 1)
        {
            (UInt32 row, UInt32 column) = context.CalcLocation(node.Position);
            throw new GrammarException("There must be an expression", row, column);
        }
        else if (elements.Count == 1)
        {
            context.Push(elements[0]);
        }
        else
        {
            AbstractDefinitionElement rightElement = elements.First();
            for (int i = 1; i < elements.Count; i++)
            {
                AbstractDefinitionElement leftElement = new OrOperator(elements[i], rightElement, context.CalcLocation, node);
                rightElement = leftElement;
            }
            context.Push(rightElement);
        }
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessSequenceNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<AbstractDefinitionElement> elements = context.PopList<AbstractDefinitionElement>();
        if (elements.Count < 1)
        {
            (UInt32 row, UInt32 column) = context.CalcLocation(node.Position);
            throw new GrammarException("There must be an expression", row, column);
        }
        else if (elements.Count == 1)
        {
            context.Push(elements[0]);
        }
        else
        {
            AbstractDefinitionElement rightElement = elements.First();
            for (int i = 1; i < elements.Count; i++)
            {
                AbstractDefinitionElement leftElement = new AndOperator(elements[i], rightElement, context.CalcLocation, node);
                rightElement = leftElement;
            }
            context.Push(rightElement);
        }
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessPrefixNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractDefinitionElement element = context.Pop<AbstractDefinitionElement>();
        AbstractMarker? marker = context.TryPop<AbstractMarker>();
        if (marker is not null)
        {
            marker.Element = element;
            element = marker;
        }
        context.Push(element);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessSuffixNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractOneChildOperator? suffixOperator = context.TryPop<AbstractOneChildOperator>();
        AbstractDefinitionElement? element = context.TryPop<AbstractDefinitionElement>();
        if ((suffixOperator is not null) && (element is null))
        {
            element = suffixOperator;
            suffixOperator = null;
        }
        if (element is null)
        {
            (UInt32 row, UInt32 column) = context.CalcLocation(node.Position);
            throw new GrammarException("Invalid tree structure!", row, column);
        }
        if (suffixOperator is not null)
        {
            suffixOperator.Element = element;
            element = suffixOperator;
        }
        context.Push(element);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessLiteralNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TextTerminal(node.Text, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessRegexNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        NumberTerminal? quantifier = context.TryPop<NumberTerminal>();
        TextTerminal expression = context.Pop<TextTerminal>();
        context.Push(new RegExTerminal(expression.Text, quantifier?.Number ?? 1, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessAllCharNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new RegExTerminal(".", 1, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessOptionalNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new OptionalOperator(null, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessZeroOrMoreNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new ZeroOrMoreOperator(null, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessOneOrMoreNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new OneOrMoreOperator(null, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public void BeginVisit(CreateCodeVisitorContext context, ASTNode node)
    {
        context.EnterScope();
    }

    public void EndVisit(CreateCodeVisitorContext context, ASTNode node)
    {
        context.ExitScope();
    }

    public CreateCodeVisitorContext GetContext(ParseResult parseResult)
    {
        return new CreateCodeVisitorContext(parseResult.InputText);
    }

    public IVisitResult GetResult(CreateCodeVisitorContext context, Boolean successful, IReadOnlyList<ParserMessage> messages)
    {
        if (context.Grammar is null)
            throw new Exception("No grammar available!");
        return new CreateCodeVisitResult(successful, context.CalcLocation, messages, context.Grammar);
    }

    public ProcessNodeResult ProcessNumberNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new NumberTerminal(Int32.Parse(node.Text), context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessCharacterClassNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TextTerminal(node.Text, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTreatInlineNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TreatInlineMarker(null, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessUseDefinitionNameAsErrorNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new UseDefinitionNameAsErrorMarker(null, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessValuePairNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractValueTerminal value = context.Pop<AbstractValueTerminal>();
        ReferenceElement? name = context.TryPop<ReferenceElement>();
        if (name is null)
        {
            name = value as ReferenceElement;
            value = new TextTerminal("", context.CalcLocation, node);
        }
        ValuePair valuePair = new ValuePair(name!.ReferenceName, value.AsText(), context.CalcLocation, node);
        context.Push(valuePair);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessBooleanNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new BooleanTerminal(Boolean.Parse(node.Text), context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMDefinitionNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        TMSequence? endSequence = context.TryPop<TMSequence>();
        TMIncludes? includes = context.TryPop<TMIncludes>();
        if ((endSequence is null) && (includes is null))
            throw new GrammarException("There must be a TextMate sequence or includes!", context.CalcLocation(node.Position));
        TMSequence? beginSequence = context.TryPop<TMSequence>();
        if (beginSequence is null)
        {
            beginSequence = endSequence;
            endSequence = null;
        }
        TMScopeName? scopeName = context.TryPop<TMScopeName>();
        ReferenceElement name = context.Pop<ReferenceElement>();
        AbstractGrammarElement newTMDefinition = new TMDefinition(name.ReferenceName, scopeName?.ScopeName, beginSequence, includes, endSequence, context.CalcLocation, node);
        context.Push(newTMDefinition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMIdentifierNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TMReferenceElement(node.Text.Trim(), context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMRegExNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TMRegExTerminal(node.Text, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMMatchNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractDefinitionElement definition = context.Pop<AbstractDefinitionElement>();
        TMScopeName? scopeName = context.TryPop<TMScopeName>();
        if (scopeName is not null)
        {
            if (definition is not TMSequence)
                definition = new TMSequence(new List<AbstractDefinitionElement>() { definition }, context.CalcLocation, node);
            (definition as TMSequence)!.ScopeName = scopeName.ScopeName;
        }
        context.Push(definition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMIncludesNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<ReferenceElement> includes = context.PopList<ReferenceElement>();
        context.Push(new TMIncludes(includes, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMMatchSequenceNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        var stackElements = context.PopList<AbstractDefinitionElement>();
        stackElements.Reverse();
        context.Push(new TMSequence(stackElements, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMScopeNameNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TMScopeName(node.Text, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessNotNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new NotOperator(null, context.CalcLocation, node));
        return ProcessNodeResult.Success;
    }

    public (UInt32 Row, UInt32 Column) CalcLocation(CreateCodeVisitorContext context, Int32 position)
    {
        return context.CalcLocation(position);
    }
}
