using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Parseidon.Parser;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace Parseidon.SourceGen;

[Generator]
public class ParseidonSourceGenerator : IIncrementalGenerator
{
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

                if (!parseResult.Successful)
                {
                    // Report errors
                    foreach (var message in parseResult.Messages)
                    {
                        var descriptor = new DiagnosticDescriptor(
                            id: "PGRAM001",
                            title: "Grammar parsing error",
                            messageFormat: "{0}",
                            category: "Parseidon",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true);

                        var diagnostic = Diagnostic.Create(
                            descriptor,
                            Location.None,
                            message.Message);

                        context.ReportDiagnostic(diagnostic);
                    }
                    continue;
                }

                // Generate code using the visitor
                CreateCodeVisitor visitor = new Parser.CreateCodeVisitor();
                var visitResult = parseResult.Visit(visitor);

                if (visitResult.Successful && visitResult is CreateCodeVisitor.IGetCode)
                {
                    // Add the generated source to the compilation
                    var sourceText = SourceText.From((visitResult as CreateCodeVisitor.IGetCode)!.Code ?? String.Empty, Encoding.UTF8);
                    context.AddSource($"{fileName}.g.cs", sourceText);
                }
                else
                {
                    // Report visitor errors
                    foreach (var message in visitResult.Messages)
                    {
                        var descriptor = new DiagnosticDescriptor(
                            id: "PGRAM002",
                            title: "Grammar code generation error",
                            messageFormat: "{0}",
                            category: "Parseidon",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true);

                        var diagnostic = Diagnostic.Create(
                            descriptor,
                            Location.None,
                            message.Message);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
            catch (Exception ex)
            {
                // Report unexpected errors
                var descriptor = new DiagnosticDescriptor(
                    id: "PGRAM999",
                    title: "Unexpected error in source generator",
                    messageFormat: "An unexpected error occurred while processing {0}: {1}",
                    category: "Parseidon",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true);

                var diagnostic = Diagnostic.Create(
                    descriptor,
                    Location.None,
                    file.Path,
                    ex.Message);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
