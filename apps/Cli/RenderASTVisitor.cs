using System.Text;
using Parseidon.Helper;
using Parseidon.Parser;
using Parseidon.Parser.Grammar;

namespace Parseidon.Cli;

public class RenderASTVisitor : IVisitor<RenderASTVisitor.RenderASTVisitorContext>
{
    public interface IGetAST
    {
        Grammar.CreateOutputResult AST { get; }
    }

    public sealed class RenderASTVisitorContext
    {
        public RenderASTVisitorContext(ParseResult parseResult)
        {
            ParseResult = parseResult;
        }

        public ParseResult ParseResult { get; }
    }

    private class RenderASTVisitorResult : IVisitResult, IGetAST
    {
        public RenderASTVisitorResult(Boolean successful, IReadOnlyList<ParserMessage> messages, Grammar.CreateOutputResult ast)
        {
            Successful = successful;
            Messages = messages;
            AST = ast;
        }

        public Boolean Successful { get; }
        public IReadOnlyList<ParserMessage> Messages { get; }
        public Grammar.CreateOutputResult AST { get; }
    }

    public RenderASTVisitorContext GetContext(ParseResult parseResult)
    {
        return new RenderASTVisitorContext(parseResult);
    }

    public IVisitResult GetResult(RenderASTVisitorContext context, bool successful, IReadOnlyList<ParserMessage> messages)
    {
        static void PrintNode(ASTNode node, bool[] crossings, StringBuilder stringBuilder)
        {
            for (int i = 0; i < crossings.Length - 1; i++)
                stringBuilder.Append(crossings[i] ? "  " : "  ");
            if (crossings.Length > 0)
                stringBuilder.Append("- ");
            stringBuilder.Append($"{node.Name}[{node.TokenId}] ({node.Position}): ");
            if (node.Text != "")
                stringBuilder.Append(node.Text.FormatLiteral(true));
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
        if (context.ParseResult is not null)
            PrintNode(context.ParseResult.RootNode!, new bool[] { }, stringBuilder);
        Grammar.CreateOutputResult outputResult = new Grammar.CreateOutputResult(true, stringBuilder.ToString(), new List<ParserMessage>());
        return new RenderASTVisitorResult(successful, messages, outputResult);
    }
}
