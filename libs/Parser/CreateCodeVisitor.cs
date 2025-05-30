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

    public override void Visit(ParseidonParser.ASTNode node)
    {
        _stack.EnterScope();
        base.Visit(node);
        _stack.ExitScope();
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

    public override void OnGrammar(ParseidonParser.ASTNode node)
    {
        List<SimpleRule> rules = PopList<SimpleRule>();
        _grammar = new Grammar.Grammar(_className, rules);
    }
    public override void OnNamespace(ParseidonParser.ASTNode node)
    {
        ReferenceElement name = Pop<ReferenceElement>();
        _namespace = name.ReferenceName;
    }
    public override void OnClassName(ParseidonParser.ASTNode node)
    {
        ReferenceElement name = Pop<ReferenceElement>();
        _className = name.ReferenceName;
    }
    public override void OnIsTerminal(ParseidonParser.ASTNode node)
    {
        Push(new IsTerminalMarker(null));
    }
    public override void OnDrop(ParseidonParser.ASTNode node)
    {
        Push(new DropMarker(null));
    }
    public override void OnSpacing(ParseidonParser.ASTNode node) { }
    public override void OnNewLine(ParseidonParser.ASTNode node) { }
    public override void OnWhiteSpace(ParseidonParser.ASTNode node) { }
    public override void OnComment(ParseidonParser.ASTNode node) { }
    public override void OnDefinition(ParseidonParser.ASTNode node)
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
    }
    public override void OnIdentifier(ParseidonParser.ASTNode node)
    {
        Push(new ReferenceElement(node.Text.Trim()));
    }
    public override void OnCSIdentifier(ParseidonParser.ASTNode node)
    {
        Push(new ReferenceElement(node.Text.Trim()));
    }    
    public override void OnIdentStart(ParseidonParser.ASTNode node) { }
    public override void OnIdentCont(ParseidonParser.ASTNode node) { }
    public override void OnExpression(ParseidonParser.ASTNode node)
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
    }
    public override void OnSequence(ParseidonParser.ASTNode node)
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
    }
    public override void OnPrefix(ParseidonParser.ASTNode node)
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
    }
    public override void OnSuffix(ParseidonParser.ASTNode node)
    {
        AbstractOneChildOperator? suffixOperator = TryPop<AbstractOneChildOperator>();
        AbstractGrammarElement element = Pop<AbstractGrammarElement>();
        if (suffixOperator is not null)
        {
            suffixOperator.Element = element;
            element = suffixOperator;
        }
        Push(element);
    }

    public override void OnPrimary(ParseidonParser.ASTNode node)
    {
        Push(new DefinitionElement(Pop<AbstractGrammarElement>()));
    }
    public override void OnLiteral(ParseidonParser.ASTNode node)
    {
        Push(new TextTerminal(node.Text));
    }
    public override void OnChar(ParseidonParser.ASTNode node) { }
    public override void OnEscapeChars(ParseidonParser.ASTNode node) { }
    public override void OnBracketOpen(ParseidonParser.ASTNode node) { }
    public override void OnBracketClose(ParseidonParser.ASTNode node) { }
    public override void OnRegex(ParseidonParser.ASTNode node)
    {
        Push(new RegExTerminal(node.Text));
    }
    public override void OnNumber(ParseidonParser.ASTNode node) { }
    public override void OnDot(ParseidonParser.ASTNode node)
    {
        Push(new RegExTerminal("."));
    }
    public override void OnOptional(ParseidonParser.ASTNode node)
    {
        Push(new OptionalOperator(null));
    }
    public override void OnZeroOrMore(ParseidonParser.ASTNode node)
    {
        Push(new ZeroOrMoreOperator(null));
    }
    public override void OnOneOrMore(ParseidonParser.ASTNode node)
    {
        Push(new OneOrMoreOperator(null));
    }
    public override void OnOr(ParseidonParser.ASTNode node) { }
    public override void OnLineEnd(ParseidonParser.ASTNode node) { }
}

