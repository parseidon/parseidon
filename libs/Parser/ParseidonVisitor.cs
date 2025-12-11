using Parseidon.Helper;
using Parseidon.Parser.Grammar;
using Parseidon.Parser.Grammar.Block;
using Parseidon.Parser.Grammar.Operators;
using Parseidon.Parser.Grammar.Terminals;

namespace Parseidon.Parser;

public class ParseidonVisitor : INodeVisitor
{
    public interface IGetResults
    {
        Grammar.Grammar.CreateOutputResult GetParserCode();
        Grammar.Grammar.CreateOutputResult GetTextMateGrammar();
        Grammar.Grammar.CreateOutputResult GetLanguageConfig();
        Grammar.Grammar.CreateOutputResult GetVSCodePackage(String? versionOverride);
    }

    private sealed class CreateCodeVisitorContext
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

        public Grammar.Grammar.CreateOutputResult GetParserCode() => _grammar.ToParserCode(MessageContext);

        public Grammar.Grammar.CreateOutputResult GetTextMateGrammar() => _grammar.ToTextMateGrammar(MessageContext);

        public Grammar.Grammar.CreateOutputResult GetLanguageConfig() => _grammar.ToLanguageConfig(MessageContext);

        public Grammar.Grammar.CreateOutputResult VSCodePackage() => _grammar.ToVSCodePackage(MessageContext, null);

        public Grammar.Grammar.CreateOutputResult GetVSCodePackage(String? versionOverride) => _grammar.ToVSCodePackage(MessageContext, versionOverride);
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

    public ProcessNodeResult ProcessGrammarNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        IEnumerable<Definition> definitions = new List<Definition>();
        IEnumerable<TMDefinition> tmDefinitions = new List<TMDefinition>();
        List<Definition> tempDefinitions = new List<Definition>();
        List<TMDefinition> tempTMDefinitions = new List<TMDefinition>();
        do
        {
            tempDefinitions = PopList<Definition>(typedContext);
            definitions = definitions.Concat(tempDefinitions);
            tempTMDefinitions = PopList<TMDefinition>(typedContext);
            tmDefinitions = tmDefinitions.Concat(tempTMDefinitions);
        } while ((tempDefinitions.Count > 0) || (tempTMDefinitions.Count > 0));
        List<ValuePair> options = PopList<ValuePair>(typedContext);
        typedContext.Grammar = new Grammar.Grammar(definitions.ToList(), tmDefinitions.ToList(), options, typedContext.MessageContext, node);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessIsTerminalNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new IsTerminalMarker(node.Children.Count == 2, null, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessDropNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new DropMarker(null, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessDefinitionNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        List<ValuePair> valuePairs = PopList<ValuePair>(typedContext);
        Dictionary<String, String> keyValuePairs = new Dictionary<String, String>();
        valuePairs.ForEach(pair => keyValuePairs.Add(pair.Name, pair.Value));
        AbstractDefinitionElement definition = Pop<AbstractDefinitionElement>(typedContext, node.Position);
        ReferenceElement name = Pop<ReferenceElement>(typedContext, node.Position);
        List<AbstractMarker> markers = new List<AbstractMarker>();
        while (TryPop<AbstractMarker>(typedContext, node.Position) is AbstractMarker marker)
        {
            markers.Add(marker);
            marker.Element = definition;
            definition = marker;
        }
        AbstractGrammarElement newDefinition = new Definition(name.ReferenceName, definition, keyValuePairs, typedContext.MessageContext, node, markers);
        Push(typedContext, newDefinition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessIdentifierNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new ReferenceElement(node.Text.Trim(), typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessCSharpIdentifierNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new ReferenceElement(node.Text.Trim(), typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessExpressionNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        List<AbstractDefinitionElement> elements = PopList<AbstractDefinitionElement>(typedContext);
        if (elements.Count < 1)
        {
            (UInt32 row, UInt32 column) = typedContext.MessageContext.CalculateLocation(node.Position);
            throw new GrammarException("There must be an expression", row, column);
        }
        else if (elements.Count == 1)
        {
            Push(typedContext, elements[0]);
        }
        else
        {
            AbstractDefinitionElement rightElement = elements.First();
            for (int i = 1; i < elements.Count; i++)
            {
                AbstractDefinitionElement leftElement = new OrOperator(elements[i], rightElement, typedContext.MessageContext, node);
                rightElement = leftElement;
            }
            Push(typedContext, rightElement);
        }
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessSequenceNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        List<AbstractDefinitionElement> elements = PopList<AbstractDefinitionElement>(typedContext);
        if (elements.Count < 1)
        {
            (UInt32 row, UInt32 column) = typedContext.MessageContext.CalculateLocation(node.Position);
            throw new GrammarException("There must be an expression", row, column);
        }
        else if (elements.Count == 1)
        {
            Push(typedContext, elements[0]);
        }
        else
        {
            AbstractDefinitionElement rightElement = elements.First();
            for (int i = 1; i < elements.Count; i++)
            {
                AbstractDefinitionElement leftElement = new AndOperator(elements[i], rightElement, typedContext.MessageContext, node);
                rightElement = leftElement;
            }
            Push(typedContext, rightElement);
        }
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessPrefixNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        AbstractDefinitionElement element = Pop<AbstractDefinitionElement>(typedContext, node.Position);
        AbstractMarker? marker = TryPop<AbstractMarker>(typedContext, node.Position);
        if (marker is not null)
        {
            marker.Element = element;
            element = marker;
        }
        Push(typedContext, element);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessSuffixNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        AbstractOneChildOperator? suffixOperator = TryPop<AbstractOneChildOperator>(typedContext, node.Position);
        AbstractDefinitionElement? element = TryPop<AbstractDefinitionElement>(typedContext, node.Position);
        if ((suffixOperator is not null) && (element is null))
        {
            element = suffixOperator;
            suffixOperator = null;
        }
        if (element is null)
        {
            (UInt32 row, UInt32 column) = typedContext.MessageContext.CalculateLocation(node.Position);
            throw new GrammarException("Invalid tree structure!", row, column);
        }
        if (suffixOperator is not null)
        {
            suffixOperator.Element = element;
            element = suffixOperator;
        }
        Push(typedContext, element);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessLiteralNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new TextTerminal(node.Text, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessRegexNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        NumberTerminal? quantifier = TryPop<NumberTerminal>(typedContext, node.Position);
        TextTerminal expression = Pop<TextTerminal>(typedContext, node.Position);
        Push(typedContext, new RegExTerminal(expression.Text, quantifier?.Number ?? 1, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessAllCharNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new RegExTerminal(".", 1, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessOptionalNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new OptionalOperator(null, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessZeroOrMoreNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new ZeroOrMoreOperator(null, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessOneOrMoreNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new OneOrMoreOperator(null, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public void BeginVisit(Object context, ASTNode node)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        typedContext.Stack.EnterScope();
    }

    public void EndVisit(Object context, ASTNode node)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        typedContext.Stack.ExitScope();
    }

    public object GetContext(ParseResult parseResult)
    {
        return new CreateCodeVisitorContext(parseResult.MessageContext);
    }

    public IVisitResult GetResult(object context, Boolean successful, IReadOnlyList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        if (typedContext.Grammar is null)
            throw new Exception("No grammar available!");
        return new CreateCodeVisitResult(successful, typedContext.MessageContext, messages, typedContext.Grammar);
    }

    public ProcessNodeResult ProcessNumberNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new NumberTerminal(Int32.Parse(node.Text), typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessCharacterClassNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new TextTerminal(node.Text, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTreatInlineNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new TreatInlineMarker(null, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessUseDefinitionNameAsErrorNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new UseDefinitionNameAsErrorMarker(null, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessValuePairNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        AbstractValueTerminal value = Pop<AbstractValueTerminal>(typedContext, node.Position);
        ReferenceElement? name = TryPop<ReferenceElement>(typedContext, node.Position);
        if (name is null)
        {
            name = value as ReferenceElement;
            value = new TextTerminal("", typedContext.MessageContext, node);
        }
        ValuePair valuePair = new ValuePair(name!.ReferenceName, value.AsText(), typedContext.MessageContext, node);
        Push(typedContext, valuePair);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessBooleanNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new BooleanTerminal(Boolean.Parse(node.Text), typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMDefinitionNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        TMSequence? endSequence = TryPop<TMSequence>(typedContext, node.Position);
        TMIncludes? includes = TryPop<TMIncludes>(typedContext, node.Position);
        if ((endSequence is null) && (includes is null))
            throw new GrammarException("There must be a TextMate sequence or includes!", typedContext.MessageContext.CalculateLocation(node.Position));
        TMSequence? beginSequence = TryPop<TMSequence>(typedContext, node.Position);
        if (beginSequence is null)
        {
            beginSequence = endSequence;
            endSequence = null;
        }
        TMScopeName? scopeName = TryPop<TMScopeName>(typedContext, node.Position);
        ReferenceElement name = Pop<ReferenceElement>(typedContext, node.Position);
        AbstractGrammarElement newTMDefinition = new TMDefinition(name.ReferenceName, scopeName?.ScopeName, beginSequence, includes, endSequence, typedContext.MessageContext, node);
        Push(typedContext, newTMDefinition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMIdentifierNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new TMReferenceElement(node.Text.Trim(), typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMRegExNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new TMRegExTerminal(node.Text, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMMatchNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        AbstractDefinitionElement definition = Pop<AbstractDefinitionElement>(typedContext, node.Position);
        TMScopeName? scopeName = TryPop<TMScopeName>(typedContext, node.Position);
        if (scopeName is not null)
        {
            if (definition is not TMSequence)
                definition = new TMSequence(new List<AbstractDefinitionElement>() { definition }, typedContext.MessageContext, node);
            (definition as TMSequence)!.ScopeName = scopeName.ScopeName;
        }
        Push(typedContext, definition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMIncludesNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        List<ReferenceElement> includes = PopList<ReferenceElement>(typedContext);
        Push(typedContext, new TMIncludes(includes, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMMatchSequenceNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        var stackElements = PopList<AbstractDefinitionElement>(typedContext);
        stackElements.Reverse();
        Push(typedContext, new TMSequence(stackElements, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMScopeNameNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new TMScopeName(node.Text, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }
}
