using System.Text;
using Parseidon.Parser.Grammar.Block;
using Parseidon.Parser.Grammar.Operators;

namespace Parseidon.Parser.Grammar;

public class Grammar : AbstractNamedElement
{
    public Grammar(String name, List<SimpleRule> rules) : base(name)
    {
        Rules = rules;
        Rules.ForEach((element) => element.Parent = this);
    }

    public List<SimpleRule> Rules { get; }

    public override String ToString(Grammar grammar) 
    {
        String result =
            $$"""
            public class {{Name}}
            {
            {{Indent(GetVisitorCode())}}

            {{Indent(GetBasicCode())}}

            {{Indent(GetCheckRuleCode())}}

            {{Indent(GetParseCode())}}
            }
            """;
        return result;        
    }
    
    public override String ToString() => ToString(this);

    public override bool IsStatic()
    {
        Boolean result = true;
        foreach (SimpleRule rule in Rules)
            result = result && rule.IsStatic();
        return result;
    }

    internal override void IterateElements(Func<AbstractGrammarElement, Boolean> process)
    {
        if(process(this))
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
        if((element is SimpleRule) && (Rules.IndexOf((SimpleRule)element) >= 0))
            return Rules.IndexOf((SimpleRule)element);
        throw new Exception($"Can not find identifier '{element.Name}'!");
    }

    public SimpleRule GetRootRule()
    {
        String? axiomName = "Grammar";
        if(axiomName == null)
            throw new Exception("Grammar must have axiom option!");
        SimpleRule? rule = FindRuleByName(axiomName);
        if (rule is null)
            throw new Exception($"Can not find axiom option '{axiomName}'!");
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
        if ((element is SimpleRule rule) && (rules.IndexOf(rule) < 0) && !(rule.Definition is DropMarker) && (!rule.IsStatic() || forceAdd))
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
    // private List<SimpleRule> GetUsedRulesWithASTNode()
    // {
    //     List<SimpleRule> result = new List<SimpleRule>();
    //     foreach(SimpleRule rule in GetUsedRules())
    //         result.Add(rule);
    //     return result;
    // }

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
                    ASTNode actualNode = new ASTNode({{GetElementIdOf(rule)}}, "{{rule.Name}}", "");
                    Boolean result = {{GetElementsCode(new List<AbstractNamedDefinitionElement>() { rule }, "Rule", null)}}
                    Int32 foundPosition = state.Position;
                    if(result && ((actualNode.Children.Count > 0) || (actualNode.Text != "")))
                        parentNode.AddChild(actualNode);
                    return result;
                }
                """;

            builder.AppendLine(ruleCode);
        }
        return builder.ToString();
    }

    protected String GetParseCode()
    {
        SimpleRule axiomRule = GetRootRule();
        String result =
            $$"""
            public void Parse(String text)
            {
                ParserState state = new ParserState(text);
                ASTNode actualNode = new ASTNode(-1, "ROOT", "");            
                if({{axiomRule.GetReferenceCode(this)}})
                    rootNode = actualNode;
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

    protected String GetVisitorCode()
    {
        List<SimpleRule> usedRules = GetRelevantGrammarRules();
        String visitorEvents = "";
        foreach(AbstractNamedElement element in usedRules)
            visitorEvents += $"public virtual void {element.GetEventName()}({Name}.ASTNode node) {{}}\n";
        String visitorCalls = "";
        foreach(AbstractNamedElement element in usedRules)
            visitorCalls += $"case {GetElementIdOf(element)}: {element.GetEventName()}(node); break;\n";

        String result =
            $$"""
            public class Visitor
            {
            {{Indent(visitorEvents)}}
                public virtual void Visit(ASTNode node)
                {
                    if(node == null)
                        return;
                    foreach(ASTNode child in node.Children)
                        Visit(child);
                    CallEvent(node.TokenId, node);
                }
            
                public virtual void CallEvent(Int32 tokenId, ASTNode node)
                {
                    switch(tokenId)
                    {
            {{Indent(Indent(Indent(visitorCalls)))}}
                    }
                }
            }
            """;
        return result;   
    }

    protected String GetBasicCode()
    {
        String result =
            $$"""
            public class ASTNode
            {
                private List<ASTNode> _children { get; } = new List<ASTNode>();
                private ASTNode? _parent = null;

                public String Text { get; set; }
                public String Name { get; set; }
                public IReadOnlyList<ASTNode> Children { get => _children; } 
                public Int32 TokenId { get; private set; }
                public Int32 Position { get; set; }
                public ASTNode? Parent { get => _parent; }            

                internal ASTNode(Int32 tokenId, String name, String text)
                {
                    Text = text;
                    TokenId = tokenId;
                    Name = name;
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
                        foreach(ASTNode child in tempChildren)
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
                    if(_parent != null)
                        _parent._children.Remove(this);
                    _parent = parent;
                    if(_parent != null)
                    {
                        if(index < 0)
                            _parent._children.Add(this);
                        else
                            _parent._children.Insert(index, this);
                    }
                }
                
                internal void AddChild(ASTNode? child)
                {
                    if(child != null)
                        _children.Add(child);
                }
            
                internal void ClearChildren()
                {
                    _children.Clear();
                }            
            }

            public class ParserState
            {
                public ParserState(String text)
                {
                    Text = text;
                }            
                public String Text { get; }
                public Int32 Position { get; set; } = 0;
                public Boolean Eof => !(Position < Text.Length);
            }

            private ASTNode? rootNode = null;

            private Boolean CheckRegEx(ASTNode parentNode, ParserState state, String regEx)
            {
                Int32 oldPosition = state.Position;
                if((state.Position < state.Text.Length) && Regex.Match(state.Text[state.Position].ToString(), regEx).Success)
                {
                    state.Position++;
                    parentNode.AddChild(new ASTNode(-1, "REGEX", state.Text.Substring(oldPosition, state.Position - oldPosition)));
                    return true;
                }
                state.Position = oldPosition;
                return false;
            }

            private Boolean CheckText(ASTNode parentNode, ParserState state, String text)
            {
                Int32 oldPosition = state.Position;
                Int32 position = 0;
                while (position < text.Length)
                {
                    if(state.Eof || (state.Text[state.Position] != text[position]))
                    {
                        state.Position = oldPosition;
                        return false;
                    }
                    position++;
                    state.Position++;
                }
                parentNode.AddChild(new ASTNode(-1, "TEXT", state.Text.Substring(oldPosition, state.Position - oldPosition)));
                return true;
            }

            private Boolean CheckAnd(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> leftCheck, Func<ASTNode, Boolean> rightCheck)
            {
                Int32 oldPosition = state.Position;
                ASTNode tempNode = new ASTNode(parentNode.TokenId, "AND", parentNode.Text);
                tempNode.Position = parentNode.Position;
                if(leftCheck(tempNode))
                {
                    if(rightCheck(tempNode))
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
                if(leftCheck(parentNode))
                    return true;
                if(rightCheck(parentNode))
                    return true;
                state.Position = oldPosition;
                return false;
            }

            private Boolean Drop(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                ASTNode tempNode = new ASTNode(-1, "", "");
                return check(tempNode);
            }

            private Boolean CheckOneOrMore(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                if(check(parentNode))
                {
                    oldPosition = state.Position;
                    while(check(parentNode))
                    {
                        oldPosition = state.Position;
                    }
                    state.Position = oldPosition;
                    return true;
                }
                state.Position = oldPosition;
                return false;
            }

            private Boolean CheckZeroOrMore(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                while((!state.Eof) && check(parentNode))
                {
                    oldPosition = state.Position;
                }
                state.Position = oldPosition;
                return true;
            }

            private Boolean CheckDifference(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> leftCheck, Func<ASTNode, Boolean> rightCheck)
            {
                Int32 oldPosition = state.Position;
                if(leftCheck(parentNode))
                {
                    state.Position = oldPosition;
                    if(rightCheck(parentNode))
                        return true;
                }
                state.Position = oldPosition;
                return false;
            }

            private Boolean CheckRange(ASTNode parentNode, ParserState state, Int32 minCount, Int32 maxCount, Func<ASTNode, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                Int32 count = 0;
                while((count <= maxCount) && check(parentNode))
                {
                    oldPosition = state.Position;
                    count++;
                }
                if(count >= minCount)
                    return true;
                state.Position = oldPosition;
                return true;
            }

            private Boolean MakeTerminal(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                ASTNode tempNode = new ASTNode(-1, "", "");
                Boolean result =  check(tempNode);
                if (result)
                {
                    tempNode.Text = tempNode.GetText();
                    tempNode.ClearChildren();
                    parentNode.Text = tempNode.GetText();
                }
                return result;
            }

            private Boolean PromoteAction(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 childCount = parentNode.Children.Count;
                Boolean result = check(parentNode);
                if(result && (childCount < parentNode.Children.Count))
                {
                    ASTNode newNode = parentNode.Children.Last();
                    parentNode.AssignFrom(newNode);
                }
                return result;
            }

            private Boolean AddVirtualNode(ASTNode parentNode, ParserState state, Int32 tokenId, String text)
            {
                parentNode.AddChild(new ASTNode(tokenId, "VIRTUAL", text));
                return true;
            }

            public void Visit(Visitor visitor)
            {
                if(rootNode == null)
                    throw new Exception("Root node is null");
                visitor.Visit(rootNode);
            }
            """;
        return result;
    }
}
