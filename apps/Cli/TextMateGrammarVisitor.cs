using Parseidon.Helper;
using Parseidon.Parser;

namespace Parseidon.Cli;

public class TextMateGrammarVisitor : INodeVisitor
{
    public interface IGetTextMateGrammar
    {
        String GrammarJson { get; }
        String LanguageConfigJson { get; }
        String PackageJson { get; }
        String Name { get; }
    }

    private class CreateTMGrammarVisitorContext
    {
    }

    private class CreateTMGrammarVisitResult : IVisitResult, IGetTextMateGrammar
    {
        public CreateTMGrammarVisitResult(Boolean successful, IReadOnlyList<ParserMessage> messages, String code, String languageConfigJson, String packageJson, String name)
        {
            Successful = successful;
            Messages = messages;
            GrammarJson = code;
            LanguageConfigJson = languageConfigJson;
            PackageJson = packageJson;
            Name = name;
        }
        public Boolean Successful { get; }
        public IReadOnlyList<ParserMessage> Messages { get; }
        public String GrammarJson { get; }
        public String LanguageConfigJson { get; }
        public String PackageJson { get; }
        public String Name { get; }
    }

    public ProcessNodeResult ProcessGrammarNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessIsTerminalNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessDropNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessDefinitionNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessIdentifierNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessCSharpIdentifierNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessExpressionNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessSequenceNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessPrefixNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessSuffixNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessLiteralNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessRegexNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessAllCharNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessOptionalNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessZeroOrMoreNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessOneOrMoreNode(Object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public void BeginVisit(Object context, ASTNode node)
    {
    }

    public void EndVisit(Object context, ASTNode node)
    {

    }

    public object GetContext(ParseResult parseResult)
    {
        return new CreateTMGrammarVisitorContext();
    }

    public IVisitResult GetResult(object context, Boolean successful, IReadOnlyList<ParserMessage> messages)
    {
        var typedContext = context as CreateTMGrammarVisitorContext ?? throw new InvalidCastException("CreateCodeVisitorContext expected!");
        return new CreateTMGrammarVisitResult(successful, messages, "", "", "", "");
    }

    public ProcessNodeResult ProcessNumberNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessCharacterClassNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessTreatInlineNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessUseRuleNameAsErrorNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessValuePairNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessBooleanNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessPropertyNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessTMDefinitionNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessTMIdentifierNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessTMMatchNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessTMRegExNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessTMMatchSequenceNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;

    public ProcessNodeResult ProcessTMIncludesNode(object context, ASTNode node, IList<ParserMessage> messages) => ProcessNodeResult.Success;
}

