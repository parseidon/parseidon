using Parseidon.Helper;
using Parseidon.Parser.Grammar;
using Parseidon.Parser.Grammar.Block;
using Parseidon.Parser.Grammar.Operators;
using Parseidon.Parser.Grammar.Terminals;

namespace Parseidon.Parser;

public class CreateCodeVisitor : ParseidonParser.Visitor
{
    private readonly string _namespace;
    private readonly string _className;
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

    public CreateCodeVisitor(String namespaceName, String className)
    {
        _namespace = namespaceName;
        _className = className;

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

    public override void OnRuleGrammar(ParseidonParser.ASTNode node)
    {
        List<SimpleRule> rules = PopList<SimpleRule>();
        _grammar = new Grammar.Grammar(_className, rules);
    }
    public override void OnRuleIsTerminal(ParseidonParser.ASTNode node)
    {
        Push(new IsTerminalMarker(null));
    }
    public override void OnRuleDrop(ParseidonParser.ASTNode node)
    {
        Push(new DropMarker(null));
    }
    public override void OnRuleSPACING(ParseidonParser.ASTNode node) { }
    public override void OnRuleNEWLINE(ParseidonParser.ASTNode node) { }
    public override void OnRuleWHITESPACE(ParseidonParser.ASTNode node) { }
    public override void OnRuleCOMMENT(ParseidonParser.ASTNode node) { }
    public override void OnRuleDefinition(ParseidonParser.ASTNode node)
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
    public override void OnRuleIdentifier(ParseidonParser.ASTNode node)
    {
        Push(new ReferenceElement(node.Text.Trim()));
    }
    public override void OnRuleIdentStart(ParseidonParser.ASTNode node) { }
    public override void OnRuleIdentCont(ParseidonParser.ASTNode node) { }
    public override void OnRuleExpression(ParseidonParser.ASTNode node)
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
    public override void OnRuleSequence(ParseidonParser.ASTNode node)
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
    public override void OnRulePrefix(ParseidonParser.ASTNode node)
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
    public override void OnRuleSuffix(ParseidonParser.ASTNode node)
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

    public override void OnRulePrimary(ParseidonParser.ASTNode node)
    {
        Push(new DefinitionElement(Pop<AbstractGrammarElement>()));
    }
    public override void OnRuleLiteral(ParseidonParser.ASTNode node)
    {
        Push(new TextTerminal(node.Text));
    }
    public override void OnRuleChar(ParseidonParser.ASTNode node) { }
    public override void OnRuleESCAPEES(ParseidonParser.ASTNode node) { }
    public override void OnRuleBRACKETOPEN(ParseidonParser.ASTNode node) { }
    public override void OnRuleBRACKETCLOSE(ParseidonParser.ASTNode node) { }
    public override void OnRuleRegex(ParseidonParser.ASTNode node)
    {
        Push(new RegExTerminal(node.Text));
    }
    public override void OnRuleNUMBER(ParseidonParser.ASTNode node) { }
    public override void OnRuleDOT(ParseidonParser.ASTNode node)
    {
        Push(new RegExTerminal("."));
    }
    public override void OnRuleQUESTION(ParseidonParser.ASTNode node)
    {
        Push(new OptionalOperator(null));
    }
    public override void OnRuleSTAR(ParseidonParser.ASTNode node)
    {
        Push(new ZeroOrMoreOperator(null));
    }
    public override void OnRulePLUS(ParseidonParser.ASTNode node)
    {
        Push(new OneOrMoreOperator(null));
    }
    public override void OnRuleSLASH(ParseidonParser.ASTNode node) { }
    public override void OnRuleLINEEND(ParseidonParser.ASTNode node) { }
}

