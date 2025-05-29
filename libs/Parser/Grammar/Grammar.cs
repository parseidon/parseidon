using System.Text;
using Parseidon.Parser.Grammar.Block;

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

    public List<SimpleRule> FindRulesByName(String name)
    {
        List<SimpleRule> rules = new List<SimpleRule>();
        foreach (SimpleRule element in Rules)
            if (element.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                rules.Add(element);
        return rules;
    }    
    
    public Int32 GetElementIdOf(AbstractNamedElement element)
    {
        if((element is SimpleRule) && (Rules.IndexOf((SimpleRule)element) >= 0))
            return Rules.IndexOf((SimpleRule)element);
        throw new Exception($"Can not find identifier '{element.Name}'!");
    }

    public SimpleRule GetAxiomRule()
    {
        String? axiomName = "Grammar";
        if(axiomName == null)
            throw new Exception("Grammar must have axiom option!");
        List<SimpleRule> rules = FindRulesByName(axiomName);
        if (rules.Count < 1)
            throw new Exception($"Can not find axiom option '{axiomName}'!");
        if (rules.Count > 1)
            throw new Exception($"There are multiple rules '{axiomName}'!");
        return rules[0];
    }

    private List<SimpleRule> GetUsedRules()
    {
        List<SimpleRule> result = new List<SimpleRule>();
        GetAxiomRule().AddUsedRules(result);
        return result;
    }

    private List<SimpleRule> GetUsedRulesWithASTNode()
    {
        List<SimpleRule> result = new List<SimpleRule>();
        foreach(SimpleRule rule in GetUsedRules())
            result.Add(rule);
        return result;
    }

    protected String GetCheckRuleCode()
    {
        StringBuilder builder = new StringBuilder();
        foreach (SimpleRule rule in GetUsedRules())
        {
            String ruleCode =
                $$"""
                public Boolean CheckRule_{{rule.Name}}(ASTNode parentNode, ParserState state)
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
        SimpleRule axiomRule = GetAxiomRule();
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
        List<SimpleRule> usedRules = GetUsedRulesWithASTNode();
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
            private ASTNode? rootNode = null;

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

                public ASTNode(Int32 tokenId, String name, String text)
                {
                    Text = text;
                    TokenId = tokenId;
                    Name = name;
                }

                public void AssignFrom(ASTNode node)
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
                 
                public String GetText()
                {
                    if (Children.Count > 0)
                        return String.Join("", Children.Select(x => x.GetText()));
                    else
                        return Text;
                }

                public void SetParent(ASTNode? parent, Int32 index = -1)
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
                
                public void AddChild(ASTNode? child)
                {
                    if(child != null)
                        _children.Add(child);
                }
            
                public void ClearChildren()
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

            public Boolean CheckRegEx(ASTNode parentNode, ParserState state, String regEx)
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

            public Boolean CheckText(ASTNode parentNode, ParserState state, String text)
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

            public Boolean CheckAnd(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> leftCheck, Func<ASTNode, Boolean> rightCheck)
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

            public Boolean CheckOr(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> leftCheck, Func<ASTNode, Boolean> rightCheck)
            {
                Int32 oldPosition = state.Position;
                if(leftCheck(parentNode))
                    return true;
                if(rightCheck(parentNode))
                    return true;
                state.Position = oldPosition;
                return false;
            }

            public Boolean Drop(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                ASTNode tempNode = new ASTNode(-1, "", "");
                return check(tempNode);
            }

            public Boolean CheckOneOrMore(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
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

            public Boolean CheckZeroOrMore(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
            {
                Int32 oldPosition = state.Position;
                while((!state.Eof) && check(parentNode))
                {
                    oldPosition = state.Position;
                }
                state.Position = oldPosition;
                return true;
            }

            public Boolean CheckDifference(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> leftCheck, Func<ASTNode, Boolean> rightCheck)
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

            public Boolean CheckRange(ASTNode parentNode, ParserState state, Int32 minCount, Int32 maxCount, Func<ASTNode, Boolean> check)
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

            public Boolean MakeTerminal(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
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

            public Boolean PromoteAction(ASTNode parentNode, ParserState state, Func<ASTNode, Boolean> check)
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

            public Boolean AddVirtualNode(ASTNode parentNode, ParserState state, Int32 tokenId, String text)
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
