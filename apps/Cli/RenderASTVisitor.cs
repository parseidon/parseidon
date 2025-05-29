using System.Text;
using Parseidon.Parser;

namespace Parseidon.Cli;

public class RenderASTVisitor : ParseidonParser.Visitor
{
    public String AST { get; private set; } = string.Empty;

    public override void Visit(ParseidonParser.ASTNode node)
    {
        static void PrintNode(ParseidonParser.ASTNode node, bool[] crossings, StringBuilder stringBuilder)
        {
            for (int i = 0; i < crossings.Length - 1; i++)
                stringBuilder.Append(crossings[i] ? "  " : "  ");
            if (crossings.Length > 0)
                stringBuilder.Append("- ");
            stringBuilder.Append($"{node.Name} ({node.TokenId}): ");
            if(node.Text != "")
                stringBuilder.Append(Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(node.Text, true));
            stringBuilder.AppendLine();
            for (int i = 0; i != node.Children.Count; i++)
            {
                bool[] childCrossings = new bool[crossings.Length + 1];
                Array.Copy(crossings, childCrossings, crossings.Length);
                childCrossings[childCrossings.Length - 1] = (i < node.Children.Count - 1);
                PrintNode(node.Children[i], childCrossings, stringBuilder);
            }
        }

        StringBuilder stringBuilder = new StringBuilder();
        PrintNode(node, new bool[] { }, stringBuilder);
        AST = stringBuilder.ToString();
    }    
}
