using Parseidon.Helper;
using Parseidon.Parser.Grammar;
using Parseidon.Parser.Grammar.Block;
using Parseidon.Parser.Grammar.Operators;
using Parseidon.Parser.Grammar.Terminals;

namespace Parseidon.Parser;

public class CreateCodeVisitor : ParseidonParser.Visitor
{
    private String _namespace = String.Empty;
    private String _className = String.Empty;
    private Grammar.Grammar? _grammar;

    private ScopedStack<AbstractGrammarElement> _stack = new ScopedStack<AbstractGrammarElement>();

    public string Code
    {
        get
        {
            if (_grammar != null)
                return $$"""
                using System.Text.RegularExpressions;

                namespace {{_namespace}};

                {{_grammar.ToString()}}
                """;
            return "";
        }
    }

    public override ParseidonParser.Visitor.ProcessNodeResult Visit(ParseidonParser.ASTNode node, IList<ParseidonParser.ParserMessage> messages)
    {
        _stack.EnterScope();
        ParseidonParser.Visitor.ProcessNodeResult result = base.Visit(node, messages);
        _stack.ExitScope();
        return result;
    }

    private T Pop<T>() where T : AbstractGrammarElement
    {
        AbstractGrammarElement lastElement = _stack.Pop();
        if (!(lastElement is T))
            throw new Exception("Expected " + typeof(T).Name + " GOT " + ((Type)lastElement.GetType()).Name);
        return (T)lastElement;
    }

    private T? TryPop<T>() where T : AbstractGrammarElement
    {
        if (_stack.TryPeek() is T)
        {
            AbstractGrammarElement lastElement = _stack.Pop();
            if (!(lastElement is T))
                throw new Exception("Expected " + typeof(T).Name + " GOT " + ((Type)lastElement.GetType()).Name);
            return (T)lastElement;
        }
        else
            return null;
    }

    private List<T> PopList<T>() where T : AbstractGrammarElement
    {
        List<T> resultList = new List<T>();
        while (_stack.TryPeek() is T)
            resultList.Add((T)_stack.Pop());
        return resultList;
    }

    private void Push(AbstractGrammarElement element)
    {
        _stack.Push(element);
    }

    public override ParseidonParser.Visitor.ProcessNodeResult ProcessGrammarNode(ParseidonParser.ASTNode node)
    {
        List<SimpleRule> rules = PopList<SimpleRule>();
        _grammar = new Grammar.Grammar(_className, rules);
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessNamespaceNode(ParseidonParser.ASTNode node)
    {
        ReferenceElement name = Pop<ReferenceElement>();
        _namespace = name.ReferenceName;
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessClassNameNode(ParseidonParser.ASTNode node)
    {
        ReferenceElement name = Pop<ReferenceElement>();
        _className = name.ReferenceName;
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessIsTerminalNode(ParseidonParser.ASTNode node)
    {
        Push(new IsTerminalMarker(null));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessDropNode(ParseidonParser.ASTNode node)
    {
        Push(new DropMarker(null));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessDefinitionNode(ParseidonParser.ASTNode node)
    {
        AbstractGrammarElement definition = Pop<AbstractGrammarElement>();
        ReferenceElement name = Pop<ReferenceElement>();
        AbstractMarker? marker = TryPop<AbstractMarker>();
        if (marker is not null)
        {
            marker.Element = definition;
            definition = marker;
        }
        Push(new SimpleRule(name.ReferenceName, definition));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessIdentifierNode(ParseidonParser.ASTNode node)
    {
        Push(new ReferenceElement(node.Text.Trim()));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessCSIdentifierNode(ParseidonParser.ASTNode node)
    {
        Push(new ReferenceElement(node.Text.Trim()));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }    
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessExpressionNode(ParseidonParser.ASTNode node)
    {
        List<AbstractDefinitionElement> elements = PopList<AbstractDefinitionElement>();
        if (elements.Count < 1)
            throw new Exception("There must be an expression");
        else if (elements.Count == 1)
        {
            Push(elements[0]);
        }
        else
        {
            AbstractGrammarElement rightElement = elements.First();
            for (int i = 1; i < elements.Count; i++)
            {
                AbstractGrammarElement leftElement = new OrOperator(elements[i], rightElement);
                rightElement = leftElement;
            }
            Push(rightElement);
        }
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessSequenceNode(ParseidonParser.ASTNode node)
    {
        List<AbstractDefinitionElement> elements = PopList<AbstractDefinitionElement>();
        if (elements.Count < 1)
            throw new Exception("There must be an expression");
        else if (elements.Count == 1)
        {
            Push(elements[0]);
        }
        else
        {
            AbstractGrammarElement rightElement = elements.First();
            for (int i = 1; i < elements.Count; i++)
            {
                AbstractGrammarElement leftElement = new AndOperator(elements[i], rightElement);
                rightElement = leftElement;
            }
            Push(rightElement);
        }
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessPrefixNode(ParseidonParser.ASTNode node)
    {
        AbstractGrammarElement element = Pop<AbstractGrammarElement>();
        AbstractMarker? marker = TryPop<AbstractMarker>();
        // element.Drop = marker is DropMarker;
        if (marker is not null)
        {
            marker.Element = element;
            element = marker;
        }
        Push(element);
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessSuffixNode(ParseidonParser.ASTNode node)
    {
        AbstractOneChildOperator? suffixOperator = TryPop<AbstractOneChildOperator>();
        AbstractGrammarElement element = Pop<AbstractGrammarElement>();
        if (suffixOperator is not null)
        {
            suffixOperator.Element = element;
            element = suffixOperator;
        }
        Push(element);
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessPrimaryNode(ParseidonParser.ASTNode node)
    {
        Push(new DefinitionElement(Pop<AbstractGrammarElement>()));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessLiteralNode(ParseidonParser.ASTNode node)
    {
        Push(new TextTerminal(node.Text));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessRegexNode(ParseidonParser.ASTNode node)
    {
        Push(new RegExTerminal(node.Text));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessDotNode(ParseidonParser.ASTNode node)
    {
        Push(new RegExTerminal("."));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessOptionalNode(ParseidonParser.ASTNode node)
    {
        Push(new OptionalOperator(null));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessZeroOrMoreNode(ParseidonParser.ASTNode node)
    {
        Push(new ZeroOrMoreOperator(null));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
    public override ParseidonParser.Visitor.ProcessNodeResult ProcessOneOrMoreNode(ParseidonParser.ASTNode node)
    {
        Push(new OneOrMoreOperator(null));
        return ParseidonParser.Visitor.ProcessNodeResult.Success;
    }
}

