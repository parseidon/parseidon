using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Parseidon.Parser;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Parseidon.SourceGen;

[Generator]
[SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008", Justification = "Source generator diagnostics are not shipped as analyzers.")]
public class ParseidonSourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor ParserErrorDescriptor = new(
        id: "PGRAM001",
        title: "Grammar parsing error",
        messageFormat: "{0}",
        category: "Parseidon",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ParserWarningDescriptor = new(
        id: "PGRAM011",
        title: "Grammar parsing warning",
        messageFormat: "{0}",
        category: "Parseidon",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor VisitorErrorDescriptor = new(
        id: "PGRAM002",
        title: "Grammar code generation error",
        messageFormat: "{0}",
        category: "Parseidon",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor VisitorWarningDescriptor = new(
        id: "PGRAM012",
        title: "Grammar code generation warning",
        messageFormat: "{0}",
        category: "Parseidon",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnexpectedErrorDescriptor = new(
        id: "PGRAM999",
        title: "Unexpected error in source generator",
        messageFormat: "An unexpected error occurred while processing {0}: {1}",
        category: "Parseidon",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all .pgram files in the project
        var pgramFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".pgram", StringComparison.OrdinalIgnoreCase));

        // Combine with the compilation
        var compilationAndFiles = context.CompilationProvider.Combine(pgramFiles.Collect());

        // Register source output
        context.RegisterSourceOutput(compilationAndFiles, static (sourceProductionContext, source) => Execute(source.Left, source.Right, sourceProductionContext));
    }

    private static void Execute(Compilation compilation, ImmutableArray<AdditionalText> files, SourceProductionContext context)
    {
        if (files.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var file in files)
        {
            var content = file.GetText(context.CancellationToken);
            if (content == null)
            {
                continue;
            }

            var grammarText = content.ToString();
            var fileName = Path.GetFileNameWithoutExtension(file.Path);

            try
            {
                // Parse the grammar file using Parseidon.Parser
                var parser = new Parser.ParseidonParser();
                var parseResult = parser.Parse(grammarText);

                bool parserReportedErrors = ReportMessages(context, content, file.Path, parseResult.Messages, ParserErrorDescriptor, ParserWarningDescriptor);

                if (!parseResult.Successful || parserReportedErrors)
                {
                    continue;
                }

                // Generate code using the visitor
                ParseidonVisitor visitor = new Parser.ParseidonVisitor();
                var visitResult = parseResult.Visit(visitor);

                bool visitorReportedErrors = ReportMessages(context, content, file.Path, visitResult.Messages, VisitorErrorDescriptor, VisitorWarningDescriptor);

                if (!visitResult.Successful || visitorReportedErrors)
                {
                    if (!visitorReportedErrors)
                    {
                        var fallbackLocation = CreateLocation(content, file.Path, 1, 1);
                        context.ReportDiagnostic(Diagnostic.Create(VisitorErrorDescriptor, fallbackLocation, "Code generation failed."));
                    }
                    continue;
                }

                if (visitResult is ParseidonVisitor.IGetResults codeResult)
                {
                    // Add the generated source to the compilation
                    var parserCodeResult = codeResult.ParserCode;
                    var sourceText = SourceText.From(parserCodeResult.Result ?? String.Empty, Encoding.UTF8);
                    context.AddSource($"{fileName}.g.cs", sourceText);
                }
                else
                {
                    var fallbackLocation = CreateLocation(content, file.Path, 1, 1);
                    context.ReportDiagnostic(Diagnostic.Create(VisitorErrorDescriptor, fallbackLocation, "Code generation did not produce any output."));
                }
            }
            catch (Exception ex)
            {
                // Report unexpected errors
                var diagnostic = Diagnostic.Create(
                    UnexpectedErrorDescriptor,
                    Location.None,
                    file.Path,
                    ex.Message);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static Boolean ReportMessages(SourceProductionContext context, SourceText sourceText, String path, IEnumerable<ParserMessage> messages, DiagnosticDescriptor errorDescriptor, DiagnosticDescriptor warningDescriptor)
    {
        Boolean hasErrors = false;
        foreach (var message in messages)
        {
            DiagnosticDescriptor descriptor = message.Type == ParserMessage.MessageType.Warning ? warningDescriptor : errorDescriptor;
            if (message.Type == ParserMessage.MessageType.Error)
                hasErrors = true;

            Location location = CreateLocation(sourceText, path, message.Row, message.Column);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, message.Message));
        }

        return hasErrors;
    }

    private static Location CreateLocation(SourceText sourceText, String path, UInt32 row, UInt32 column)
    {
        if ((row == 0) || (column == 0) || (sourceText.Lines.Count == 0))
        {
            return Location.Create(path, new TextSpan(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
        }

        Int32 lineIndex = (Int32)Math.Min((Int64)row - 1, sourceText.Lines.Count - 1);
        TextLine line = sourceText.Lines[lineIndex];
        Int32 zeroBasedColumn = (Int32)Math.Min((Int64)column - 1, line.Span.Length);
        Int32 position = line.Start + zeroBasedColumn;

        LinePosition linePosition = new LinePosition(lineIndex, zeroBasedColumn);
        TextSpan span = new TextSpan(position, 0);
        LinePositionSpan lineSpan = new LinePositionSpan(linePosition, linePosition);
        return Location.Create(path, span, lineSpan);
    }
}
