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

    public sealed class CreateCodeVisitorContext
    {
        public CreateCodeVisitorContext(MessageContext messageContext)
        {
            MessageContext = messageContext;
        }

        internal ScopedStack<AbstractGrammarElement> Stack { get; } = new ScopedStack<AbstractGrammarElement>();
        internal Grammar.Grammar? Grammar { get; set; }
        internal MessageContext MessageContext { get; set; }
    }

    private class CreateCodeVisitResult : IVisitResult, IGetResults
    {
        public CreateCodeVisitResult(Boolean successful, MessageContext messageContext, IReadOnlyList<ParserMessage> messages, Grammar.Grammar grammar)
        {
            Successful = successful;
            MessageContext = messageContext;
            Messages = messages;
            _grammar = grammar;
        }

        private Grammar.Grammar _grammar;
        private MessageContext MessageContext;
        public Boolean Successful { get; }
        public IReadOnlyList<ParserMessage> Messages { get; }

        public Grammar.Grammar.CreateOutputResult GetParserCode(String? namespaceOverride = null, String? classOverride = null, Boolean? generateNodeVisitorOverride = null) => _grammar.ToParserCode(MessageContext, namespaceOverride, classOverride, generateNodeVisitorOverride);

        public Grammar.Grammar.CreateOutputResult GetTextMateGrammar() => _grammar.ToTextMateGrammar(MessageContext);

        public Grammar.Grammar.CreateOutputResult GetLanguageConfig() => _grammar.ToLanguageConfig(MessageContext);

        public Grammar.Grammar.CreateOutputResult GetVSCodePackage(String? versionOverride, Func<String, String>? loadMergeJson, String? packageJsonOverridePath) => _grammar.ToVSCodePackage(MessageContext, versionOverride, loadMergeJson, packageJsonOverridePath);
    }

    private T Pop<T>(CreateCodeVisitorContext context, Int32 position) where T : AbstractGrammarElement
    {
        AbstractGrammarElement lastElement = context.Stack.Pop();
        if (!(lastElement is T))
        {
            (UInt32 row, UInt32 column) = context.MessageContext.CalculateLocation(position);
            throw new GrammarException($"Expected {typeof(T).Name}, got {((Type)lastElement.GetType()).Name}!", row, column);
        }
        return (T)lastElement;
    }

    private T? TryPop<T>(CreateCodeVisitorContext context, Int32 position) where T : AbstractGrammarElement
    {
        if (context.Stack.TryPeek() is T)
            return Pop<T>(context, position);
        else
            return null;
    }

    private List<T> PopList<T>(CreateCodeVisitorContext context) where T : AbstractGrammarElement
    {
        List<T> resultList = new List<T>();
        while (context.Stack.TryPeek() is T)
            resultList.Add((T)context.Stack.Pop());
        return resultList;
    }

    private void Push(CreateCodeVisitorContext context, AbstractGrammarElement element)
    {
        context.Stack.Push(element);
    }

    public ProcessNodeResult ProcessGrammarNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        IEnumerable<Definition> definitions = new List<Definition>();
        IEnumerable<TMDefinition> tmDefinitions = new List<TMDefinition>();
        List<Definition> tempDefinitions = new List<Definition>();
        List<TMDefinition> tempTMDefinitions = new List<TMDefinition>();
        do
        {
            tempDefinitions = PopList<Definition>(context);
            definitions = definitions.Concat(tempDefinitions);
            tempTMDefinitions = PopList<TMDefinition>(context);
            tmDefinitions = tmDefinitions.Concat(tempTMDefinitions);
        } while ((tempDefinitions.Count > 0) || (tempTMDefinitions.Count > 0));
        List<ValuePair> options = PopList<ValuePair>(context);
        context.Grammar = new Grammar.Grammar(definitions.ToList(), tmDefinitions.ToList(), options, context.MessageContext, node);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessIsTerminalNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new IsTerminalMarker(node.Children.Count == 2, null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessDropNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new DropMarker(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessDefinitionNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<ValuePair> valuePairs = PopList<ValuePair>(context);
        AbstractDefinitionElement definition = Pop<AbstractDefinitionElement>(context, node.Position);
        ReferenceElement name = Pop<ReferenceElement>(context, node.Position);
        List<AbstractMarker> markers = new List<AbstractMarker>();
        while (TryPop<AbstractMarker>(context, node.Position) is AbstractMarker marker)
        {
            markers.Add(marker);
            marker.Element = definition;
            definition = marker;
        }
        AbstractGrammarElement newDefinition = new Definition(name.ReferenceName, definition, valuePairs, context.MessageContext, node, markers);
        Push(context, newDefinition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessIdentifierNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new ReferenceElement(node.Text.Trim(), context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessCSharpIdentifierNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new ReferenceElement(node.Text.Trim(), context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessExpressionNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<AbstractDefinitionElement> elements = PopList<AbstractDefinitionElement>(context);
        if (elements.Count < 1)
        {
            (UInt32 row, UInt32 column) = context.MessageContext.CalculateLocation(node.Position);
            throw new GrammarException("There must be an expression", row, column);
        }
        else if (elements.Count == 1)
        {
            Push(context, elements[0]);
        }
        else
        {
            AbstractDefinitionElement rightElement = elements.First();
            for (int i = 1; i < elements.Count; i++)
            {
                AbstractDefinitionElement leftElement = new OrOperator(elements[i], rightElement, context.MessageContext, node);
                rightElement = leftElement;
            }
            Push(context, rightElement);
        }
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessSequenceNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<AbstractDefinitionElement> elements = PopList<AbstractDefinitionElement>(context);
        if (elements.Count < 1)
        {
            (UInt32 row, UInt32 column) = context.MessageContext.CalculateLocation(node.Position);
            throw new GrammarException("There must be an expression", row, column);
        }
        else if (elements.Count == 1)
        {
            Push(context, elements[0]);
        }
        else
        {
            AbstractDefinitionElement rightElement = elements.First();
            for (int i = 1; i < elements.Count; i++)
            {
                AbstractDefinitionElement leftElement = new AndOperator(elements[i], rightElement, context.MessageContext, node);
                rightElement = leftElement;
            }
            Push(context, rightElement);
        }
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessPrefixNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractDefinitionElement element = Pop<AbstractDefinitionElement>(context, node.Position);
        AbstractMarker? marker = TryPop<AbstractMarker>(context, node.Position);
        if (marker is not null)
        {
            marker.Element = element;
            element = marker;
        }
        Push(context, element);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessSuffixNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractOneChildOperator? suffixOperator = TryPop<AbstractOneChildOperator>(context, node.Position);
        AbstractDefinitionElement? element = TryPop<AbstractDefinitionElement>(context, node.Position);
        if ((suffixOperator is not null) && (element is null))
        {
            element = suffixOperator;
            suffixOperator = null;
        }
        if (element is null)
        {
            (UInt32 row, UInt32 column) = context.MessageContext.CalculateLocation(node.Position);
            throw new GrammarException("Invalid tree structure!", row, column);
        }
        if (suffixOperator is not null)
        {
            suffixOperator.Element = element;
            element = suffixOperator;
        }
        Push(context, element);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessLiteralNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new TextTerminal(node.Text, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessRegexNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        NumberTerminal? quantifier = TryPop<NumberTerminal>(context, node.Position);
        TextTerminal expression = Pop<TextTerminal>(context, node.Position);
        Push(context, new RegExTerminal(expression.Text, quantifier?.Number ?? 1, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessAllCharNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new RegExTerminal(".", 1, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessOptionalNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new OptionalOperator(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessZeroOrMoreNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new ZeroOrMoreOperator(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessOneOrMoreNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new OneOrMoreOperator(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public void BeginVisit(CreateCodeVisitorContext context, ASTNode node)
    {
        context.Stack.EnterScope();
    }

    public void EndVisit(CreateCodeVisitorContext context, ASTNode node)
    {
        context.Stack.ExitScope();
    }

    public CreateCodeVisitorContext GetContext(ParseResult parseResult)
    {
        return new CreateCodeVisitorContext(parseResult.MessageContext);
    }

    public IVisitResult GetResult(CreateCodeVisitorContext context, Boolean successful, IReadOnlyList<ParserMessage> messages)
    {
        if (context.Grammar is null)
            throw new Exception("No grammar available!");
        return new CreateCodeVisitResult(successful, context.MessageContext, messages, context.Grammar);
    }

    public ProcessNodeResult ProcessNumberNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new NumberTerminal(Int32.Parse(node.Text), context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessCharacterClassNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new TextTerminal(node.Text, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTreatInlineNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new TreatInlineMarker(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessUseDefinitionNameAsErrorNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new UseDefinitionNameAsErrorMarker(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessValuePairNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractValueTerminal value = Pop<AbstractValueTerminal>(context, node.Position);
        ReferenceElement? name = TryPop<ReferenceElement>(context, node.Position);
        if (name is null)
        {
            name = value as ReferenceElement;
            value = new TextTerminal("", context.MessageContext, node);
        }
        ValuePair valuePair = new ValuePair(name!.ReferenceName, value.AsText(), context.MessageContext, node);
        Push(context, valuePair);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessBooleanNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new BooleanTerminal(Boolean.Parse(node.Text), context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMDefinitionNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        TMSequence? endSequence = TryPop<TMSequence>(context, node.Position);
        TMIncludes? includes = TryPop<TMIncludes>(context, node.Position);
        if ((endSequence is null) && (includes is null))
            throw new GrammarException("There must be a TextMate sequence or includes!", context.MessageContext.CalculateLocation(node.Position));
        TMSequence? beginSequence = TryPop<TMSequence>(context, node.Position);
        if (beginSequence is null)
        {
            beginSequence = endSequence;
            endSequence = null;
        }
        TMScopeName? scopeName = TryPop<TMScopeName>(context, node.Position);
        ReferenceElement name = Pop<ReferenceElement>(context, node.Position);
        AbstractGrammarElement newTMDefinition = new TMDefinition(name.ReferenceName, scopeName?.ScopeName, beginSequence, includes, endSequence, context.MessageContext, node);
        Push(context, newTMDefinition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMIdentifierNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new TMReferenceElement(node.Text.Trim(), context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMRegExNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new TMRegExTerminal(node.Text, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMMatchNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractDefinitionElement definition = Pop<AbstractDefinitionElement>(context, node.Position);
        TMScopeName? scopeName = TryPop<TMScopeName>(context, node.Position);
        if (scopeName is not null)
        {
            if (definition is not TMSequence)
                definition = new TMSequence(new List<AbstractDefinitionElement>() { definition }, context.MessageContext, node);
            (definition as TMSequence)!.ScopeName = scopeName.ScopeName;
        }
        Push(context, definition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMIncludesNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<ReferenceElement> includes = PopList<ReferenceElement>(context);
        Push(context, new TMIncludes(includes, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMMatchSequenceNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        var stackElements = PopList<AbstractDefinitionElement>(context);
        stackElements.Reverse();
        Push(context, new TMSequence(stackElements, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMScopeNameNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new TMScopeName(node.Text, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessNotNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        Push(context, new NotOperator(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }
}
