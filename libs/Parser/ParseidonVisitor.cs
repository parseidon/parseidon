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

        private ScopedStack<AbstractGrammarElement> Stack { get; } = new ScopedStack<AbstractGrammarElement>();
        internal Grammar.Grammar? Grammar { get; set; }
        internal MessageContext MessageContext { get; set; }

        internal T Pop<T>(Int32 position) where T : AbstractGrammarElement
        {
            try
            {
                return Stack.Pop<T>();
            }
            catch (Exception e)
            {
                (UInt32 row, UInt32 column) = MessageContext.CalculateLocation(position);
                throw new GrammarException(e.Message, row, column);
            }
        }

        internal T? TryPop<T>(Int32 position) where T : AbstractGrammarElement
        {
            return Stack.TryPop<T>();
        }

        internal List<T> PopList<T>() where T : AbstractGrammarElement
        {
            return Stack.PopList<T>();
        }

        internal void Push(AbstractGrammarElement element)
        {
            Stack.Push(element);
        }

        internal void EnterScope()
        {
            Stack.EnterScope();
        }

        internal void ExitScope()
        {
            Stack.ExitScope();
        }
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
        context.Grammar = new Grammar.Grammar(definitions.ToList(), tmDefinitions.ToList(), options, context.MessageContext, node);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessIsTerminalNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new IsTerminalMarker(node.Children.Count == 2, null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessDropNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new DropMarker(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessDefinitionNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<ValuePair> valuePairs = context.PopList<ValuePair>();
        AbstractDefinitionElement definition = context.Pop<AbstractDefinitionElement>(node.Position);
        ReferenceElement name = context.Pop<ReferenceElement>(node.Position);
        List<AbstractMarker> markers = new List<AbstractMarker>();
        while (context.TryPop<AbstractMarker>(node.Position) is AbstractMarker marker)
        {
            markers.Add(marker);
            marker.Element = definition;
            definition = marker;
        }
        AbstractGrammarElement newDefinition = new Definition(name.ReferenceName, definition, valuePairs, context.MessageContext, node, markers);
        context.Push(newDefinition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessIdentifierNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new ReferenceElement(node.Text.Trim(), context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessCSharpIdentifierNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new ReferenceElement(node.Text.Trim(), context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessExpressionNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<AbstractDefinitionElement> elements = context.PopList<AbstractDefinitionElement>();
        if (elements.Count < 1)
        {
            (UInt32 row, UInt32 column) = context.MessageContext.CalculateLocation(node.Position);
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
                AbstractDefinitionElement leftElement = new OrOperator(elements[i], rightElement, context.MessageContext, node);
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
            (UInt32 row, UInt32 column) = context.MessageContext.CalculateLocation(node.Position);
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
                AbstractDefinitionElement leftElement = new AndOperator(elements[i], rightElement, context.MessageContext, node);
                rightElement = leftElement;
            }
            context.Push(rightElement);
        }
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessPrefixNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractDefinitionElement element = context.Pop<AbstractDefinitionElement>(node.Position);
        AbstractMarker? marker = context.TryPop<AbstractMarker>(node.Position);
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
        AbstractOneChildOperator? suffixOperator = context.TryPop<AbstractOneChildOperator>(node.Position);
        AbstractDefinitionElement? element = context.TryPop<AbstractDefinitionElement>(node.Position);
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
        context.Push(element);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessLiteralNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TextTerminal(node.Text, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessRegexNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        NumberTerminal? quantifier = context.TryPop<NumberTerminal>(node.Position);
        TextTerminal expression = context.Pop<TextTerminal>(node.Position);
        context.Push(new RegExTerminal(expression.Text, quantifier?.Number ?? 1, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessAllCharNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new RegExTerminal(".", 1, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessOptionalNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new OptionalOperator(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessZeroOrMoreNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new ZeroOrMoreOperator(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessOneOrMoreNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new OneOrMoreOperator(null, context.MessageContext, node));
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
        context.Push(new NumberTerminal(Int32.Parse(node.Text), context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessCharacterClassNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TextTerminal(node.Text, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTreatInlineNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TreatInlineMarker(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessUseDefinitionNameAsErrorNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new UseDefinitionNameAsErrorMarker(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessValuePairNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractValueTerminal value = context.Pop<AbstractValueTerminal>(node.Position);
        ReferenceElement? name = context.TryPop<ReferenceElement>(node.Position);
        if (name is null)
        {
            name = value as ReferenceElement;
            value = new TextTerminal("", context.MessageContext, node);
        }
        ValuePair valuePair = new ValuePair(name!.ReferenceName, value.AsText(), context.MessageContext, node);
        context.Push(valuePair);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessBooleanNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new BooleanTerminal(Boolean.Parse(node.Text), context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMDefinitionNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        TMSequence? endSequence = context.TryPop<TMSequence>(node.Position);
        TMIncludes? includes = context.TryPop<TMIncludes>(node.Position);
        if ((endSequence is null) && (includes is null))
            throw new GrammarException("There must be a TextMate sequence or includes!", context.MessageContext.CalculateLocation(node.Position));
        TMSequence? beginSequence = context.TryPop<TMSequence>(node.Position);
        if (beginSequence is null)
        {
            beginSequence = endSequence;
            endSequence = null;
        }
        TMScopeName? scopeName = context.TryPop<TMScopeName>(node.Position);
        ReferenceElement name = context.Pop<ReferenceElement>(node.Position);
        AbstractGrammarElement newTMDefinition = new TMDefinition(name.ReferenceName, scopeName?.ScopeName, beginSequence, includes, endSequence, context.MessageContext, node);
        context.Push(newTMDefinition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMIdentifierNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TMReferenceElement(node.Text.Trim(), context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMRegExNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TMRegExTerminal(node.Text, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMMatchNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        AbstractDefinitionElement definition = context.Pop<AbstractDefinitionElement>(node.Position);
        TMScopeName? scopeName = context.TryPop<TMScopeName>(node.Position);
        if (scopeName is not null)
        {
            if (definition is not TMSequence)
                definition = new TMSequence(new List<AbstractDefinitionElement>() { definition }, context.MessageContext, node);
            (definition as TMSequence)!.ScopeName = scopeName.ScopeName;
        }
        context.Push(definition);
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMIncludesNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        List<ReferenceElement> includes = context.PopList<ReferenceElement>();
        context.Push(new TMIncludes(includes, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMMatchSequenceNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        var stackElements = context.PopList<AbstractDefinitionElement>();
        stackElements.Reverse();
        context.Push(new TMSequence(stackElements, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessTMScopeNameNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new TMScopeName(node.Text, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }

    public ProcessNodeResult ProcessNotNode(CreateCodeVisitorContext context, ASTNode node, IList<ParserMessage> messages)
    {
        context.Push(new NotOperator(null, context.MessageContext, node));
        return ProcessNodeResult.Success;
    }
}
