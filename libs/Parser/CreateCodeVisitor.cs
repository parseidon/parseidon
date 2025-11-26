using Parseidon.Helper;
using Parseidon.Parser.Grammar;
using Parseidon.Parser.Grammar.Block;
using Parseidon.Parser.Grammar.Operators;
using Parseidon.Parser.Grammar.Terminals;

namespace Parseidon.Parser;

public class CreateCodeVisitor : INodeVisitor
{
    public interface IGetCode
    {
        String? Code { get; }
    }

    private class CreateCodeVisitorContext
    {
        public CreateCodeVisitorContext(MessageContext messageContext)
        {
            MessageContext = messageContext;
        }

        internal ScopedStack<AbstractGrammarElement> Stack { get; } = new ScopedStack<AbstractGrammarElement>();
        internal Grammar.Grammar? Grammar { get; set; }
        internal MessageContext MessageContext { get; set; }
    }

    private class CreateCodeVisitResult : IVisitResult, IGetCode
    {
        public CreateCodeVisitResult(Boolean successful, IReadOnlyList<ParserMessage> messages, String? code)
        {
            Successful = successful;
            Messages = messages;
            Code = code;
        }

        public Boolean Successful { get; }
        public IReadOnlyList<ParserMessage> Messages { get; }
        public String? Code { get; }
    }

    private T Pop<T>(CreateCodeVisitorContext context) where T : AbstractGrammarElement
    {
        AbstractGrammarElement lastElement = context.Stack.Pop();
        if (!(lastElement is T))
            throw new Exception("Expected " + typeof(T).Name + " GOT " + ((Type)lastElement.GetType()).Name);
        return (T)lastElement;
    }

    private T? TryPop<T>(CreateCodeVisitorContext context) where T : AbstractGrammarElement
    {
        if (context.Stack.TryPeek() is T)
        {
            AbstractGrammarElement lastElement = context.Stack.Pop();
            if (!(lastElement is T))
                throw new Exception("Expected " + typeof(T).Name + " GOT " + ((Type)lastElement.GetType()).Name);
            return (T)lastElement;
        }
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
        List<SimpleRule> rules = PopList<SimpleRule>(typedContext);
        List<ValuePair> options = PopList<ValuePair>(typedContext);
        typedContext.Grammar = new Grammar.Grammar(rules, options, typedContext.MessageContext, node);
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
        AbstractDefinitionElement definition = Pop<AbstractDefinitionElement>(typedContext);
        ReferenceElement name = Pop<ReferenceElement>(typedContext);
        List<AbstractMarker> markers = new List<AbstractMarker>();
        while (TryPop<AbstractMarker>(typedContext) is AbstractMarker marker)
        {
            markers.Add(marker);
            marker.Element = definition;
            definition = marker;
        }
        AbstractGrammarElement newRule = new SimpleRule(name.ReferenceName, definition, keyValuePairs, typedContext.MessageContext, node, markers);
        Push(typedContext, newRule);
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
            AbstractGrammarElement rightElement = elements.First();
            for (int i = 1; i < elements.Count; i++)
            {
                AbstractGrammarElement leftElement = new OrOperator(elements[i], rightElement, typedContext.MessageContext, node);
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
            AbstractGrammarElement rightElement = elements.First();
            for (int i = 1; i < elements.Count; i++)
            {
                AbstractGrammarElement leftElement = new AndOperator(elements[i], rightElement, typedContext.MessageContext, node);
                rightElement = leftElement;
            }
            Push(typedContext, rightElement);
        }
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessPrefixNode(Object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        AbstractGrammarElement element = Pop<AbstractGrammarElement>(typedContext);
        AbstractMarker? marker = TryPop<AbstractMarker>(typedContext);
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
        AbstractOneChildOperator? suffixOperator = TryPop<AbstractOneChildOperator>(typedContext);
        AbstractGrammarElement element = Pop<AbstractGrammarElement>(typedContext);
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
        NumberTerminal? quantifier = TryPop<NumberTerminal>(typedContext);
        TextTerminal expression = Pop<TextTerminal>(typedContext);
        Push(typedContext, new RegExTerminal(expression.Text, quantifier?.Number ?? 1, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessDotNode(Object context, ASTNode node, IList<ParserMessage> messages)
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
        return new CreateCodeVisitResult(successful, messages, typedContext.Grammar is not null ? typedContext.Grammar.ToString() : String.Empty);
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

    public ProcessNodeResult ProcessUseRuleNameAsErrorNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new UseRuleNameAsErrorMarker(null, typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessValuePairNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        AbstractValueTerminal value = Pop<AbstractValueTerminal>(typedContext);
        ReferenceElement name = Pop<ReferenceElement>(typedContext);
        ValuePair valuePair = new ValuePair(name.ReferenceName, value.AsText(), typedContext.MessageContext, node);
        Push(typedContext, valuePair);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessBooleanNode(object context, ASTNode node, IList<ParserMessage> messages)
    {
        var typedContext = context as CreateCodeVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        Push(typedContext, new BooleanTerminal(Boolean.Parse(node.Text), typedContext.MessageContext, node));
        return ProcessNodeResult.Success;
    }
}

