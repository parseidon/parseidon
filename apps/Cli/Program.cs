using Parseidon.Parser;
using Spectre.Console;
using System.CommandLine;
using Parseidon.Cli;
using Parseidon.Parser.Grammar;
using Parseidon.Helper;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Text;
using System.Linq;

var rootCommand = new RootCommand("Parser generator for .NET");

var overrideOption = new Option<String>("--override", "-o")
{
    Description = "How to handle if the output file already exists",
    DefaultValueFactory = _ => "ask",
    Recursive = true
};
overrideOption.AcceptOnlyFromAmong("ask", "abort", "backup", "override");
rootCommand.Add(overrideOption);

var parseCommand = new Command("parser")
{
    Description = "Create parser class for the grammar as a C# class"
};
rootCommand.Add(parseCommand);

var parserNamespaceOption = new Option<String?>("--namespace", "-n")
{
    Description = "Override the namespace for the generated parser",
    DefaultValueFactory = _ => null
};
parseCommand.Add(parserNamespaceOption);

var parserClassNameOption = new Option<String?>("--class-name", "-c")
{
    Description = "Override the class name for the generated parser",
    DefaultValueFactory = _ => null
};
parseCommand.Add(parserClassNameOption);

var astCommand = new Command("ast")
{
    Description = "Create an AST for a code file using a parser generated from the grammar"
};
rootCommand.Add(astCommand);

var textMateCommand = new Command("textmate")
{
    Description = "Create a TextMate grammar from the provided grammar definition"
};
rootCommand.Add(textMateCommand);

var vsCodeCommand = new Command("vscode")
{
    Description = "Create a VS Code extension with the syntax highlighting from the provided grammar definition"
};
rootCommand.Add(vsCodeCommand);

var versionOption = new Option<String?>("--version", "-v")
{
    Description = "Override the version number in package.json",
    DefaultValueFactory = _ => null
};
vsCodeCommand.Add(versionOption);

var packageJsonOverrideOption = new Option<FileInfo?>("--package-json-override", "-p")
{
    Description = "JSON file to merge into package.json; overrides on conflicts",
    DefaultValueFactory = _ => null
};
vsCodeCommand.Add(packageJsonOverrideOption);

var grammarFileArgument = new Argument<FileInfo>(name: "GRAMMAR-FILE")
{
    Description = "The grammar file to be used"
};
grammarFileArgument.AcceptLegalFilePathsOnly();
parseCommand.Add(grammarFileArgument);
astCommand.Add(grammarFileArgument);
textMateCommand.Add(grammarFileArgument);
vsCodeCommand.Add(grammarFileArgument);

var codeFileArgument = new Argument<FileInfo>(name: "CODE-FILE")
{
    Description = "The input file to parse into an AST"
};
codeFileArgument.AcceptLegalFilePathsOnly();
astCommand.Add(codeFileArgument);

var outputFileArgument = new Argument<FileInfo>(name: "OUTPUT-FILE")
{
    Description = "The output file"
};
parseCommand.Add(outputFileArgument);
astCommand.Add(outputFileArgument);
textMateCommand.Add(outputFileArgument);

var outputFolderArgument = new Argument<DirectoryInfo>(name: "OUTPUT-FOLDER")
{
    Description = "The output folder"
};
vsCodeCommand.Add(outputFolderArgument);

parseCommand.SetAction((parseResult) =>
    {
        FileInfo grammarFile = parseResult.GetValue(grammarFileArgument)!;
        FileInfo outputFile = parseResult.GetValue(outputFileArgument)!;
        String doOverride = parseResult.GetValue<String>(overrideOption)!;
        String? namespaceOverride = parseResult.GetValue<String?>(parserNamespaceOption);
        String? classNameOverride = parseResult.GetValue<String?>(parserClassNameOption);
        return RunParser(
            grammarFile,
            () => ValidateFileInput(grammarFile, outputFile, doOverride),
            (result) => CreateParser(result, outputFile, doOverride, namespaceOverride, classNameOverride)
        );
    }
);

astCommand.SetAction((parseResult) =>
    {
        FileInfo grammarFile = parseResult.GetValue(grammarFileArgument)!;
        FileInfo codeFile = parseResult.GetValue(codeFileArgument)!;
        FileInfo outputFile = parseResult.GetValue(outputFileArgument)!;
        String doOverride = parseResult.GetValue<String>(overrideOption)!;
        return RunAstGeneration(grammarFile, codeFile, outputFile, doOverride);
    }
);

textMateCommand.SetAction((parseResult) =>
    {
        FileInfo grammarFile = parseResult.GetValue(grammarFileArgument)!;
        FileInfo outputFile = parseResult.GetValue(outputFileArgument)!;
        String doOverride = parseResult.GetValue<String>(overrideOption)!;
        return RunParser(
            grammarFile,
            () => ValidateFileInput(grammarFile, outputFile, doOverride),
            (result) => CreateTextMateGrammar(result, outputFile, doOverride)
        );
    }
);

vsCodeCommand.SetAction((parseResult) =>
    {
        FileInfo grammarFile = parseResult.GetValue(grammarFileArgument)!;
        DirectoryInfo outputFolder = parseResult.GetValue(outputFolderArgument)!;
        String doOverride = parseResult.GetValue<String>(overrideOption)!;
        String? version = parseResult.GetValue<String?>(versionOption);
        FileInfo? packageOverrideFile = parseResult.GetValue<FileInfo?>(packageJsonOverrideOption);
        if (packageOverrideFile is not null && !packageOverrideFile.Exists)
        {
            PrintMessage(ParserMessage.MessageType.Error, $"The file '{packageOverrideFile.FullName}' could not be found!");
            return 1;
        }
        return RunParser(
            grammarFile,
            () => ValidateFolderInput(grammarFile, outputFolder, doOverride),
            (result) => CreateVSCodePackage(result, outputFolder, doOverride, grammarFile, version, packageOverrideFile)
        );
    }
);

System.CommandLine.ParseResult parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();

static int RunParser(FileInfo grammarFile, Func<Int32> validateInput, Func<Parseidon.Parser.ParseResult, OutputResult> processResult)
{
    Int32 exitCode = validateInput();
    if (exitCode != 0)
        return exitCode;

    ParseidonParser Parser = new ParseidonParser();
    Parseidon.Parser.ParseResult parseResult = Parser.Parse(File.ReadAllText(grammarFile.FullName));
    OutputResult? outputResult = null;
    if (parseResult.Successful)
        outputResult = processResult(parseResult);

    int parseResultExitCode = ProcessMessages(parseResult.Messages);
    int visitResultExitCode = ProcessMessages(outputResult?.VisitorMessages);
    int createOutputResultExitCode = ProcessMessages(outputResult?.CreateOutputMessages);

    // Exit Code 1 wenn einer der beiden Fehler hatte
    if (visitResultExitCode != 0 || parseResultExitCode != 0 || createOutputResultExitCode != 0)
        return 1;

    return 0;
}

static int RunAstGeneration(FileInfo grammarFile, FileInfo codeFile, FileInfo outputFile, String overrideOption)
{
    Int32 exitCode = ValidateAstInput(grammarFile, codeFile, outputFile, overrideOption);
    if (exitCode != 0)
        return exitCode;

    OutputResult outputResult = CreateAST(grammarFile, codeFile, outputFile, overrideOption);
    int visitResultExitCode = ProcessMessages(outputResult.VisitorMessages);
    int createOutputResultExitCode = ProcessMessages(outputResult.CreateOutputMessages);

    if (!outputResult.Successful || visitResultExitCode != 0 || createOutputResultExitCode != 0)
        return 1;

    AnsiConsole.MarkupLine($"[green] The AST-file '{outputFile.FullName}' is successfully created![/]");
    return 0;
}

static int ProcessMessages(IReadOnlyList<ParserMessage>? messages)
{
    if (messages == null || messages.Count == 0)
        return 0;

    bool hasErrors = false;
    foreach (var message in messages)
    {
        PrintMessage(message.Type, $"({message.Row}:{message.Column}) {message.Message}");
        if (message.Type == ParserMessage.MessageType.Error)
            hasErrors = true;
    }

    return hasErrors ? 1 : 0;
}

static void PrintMessage(ParserMessage.MessageType messageType, String message)
{
    var color = messageType == ParserMessage.MessageType.Error ? "red" : "yellow";
    var messageTypeText = messageType == ParserMessage.MessageType.Error ? "ERROR" : "WARNING";
    AnsiConsole.MarkupLine($"[{color}]{messageTypeText}: {Markup.Escape(message)}[/]");
}

static OutputResult CreateParser(Parseidon.Parser.ParseResult parseResult, FileInfo outputFile, String overrideOption, String? namespaceOverride, String? classNameOverride)
{
    IVisitor visitor = new ParseidonVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);

    Grammar.CreateOutputResult outputResult = Grammar.CreateOutputResult.Empty;
    if (visitResult.Successful && visitResult is ParseidonVisitor.IGetResults typedVisitResult)
    {
        outputResult = typedVisitResult.GetParserCode(namespaceOverride, classNameOverride);
        if (outputResult.Successful)
        {
            var code =
                $"""
                //****************************************//
                //*                                      *//
                //* This code is generated by parseidon. *//
                //*     https://github.com/parseidon     *//
                //*                                      *//
                //*         {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}          *//
                //*                                      *//
                //****************************************//

                {outputResult.Output}
                """;
            if (outputFile.Exists && overrideOption.Equals("backup"))
            {
                Int32 backupFileNo = 1;
                while (File.Exists($"{outputFile.FullName}.{backupFileNo}.bak"))
                    backupFileNo++;
                File.Copy(outputFile.FullName, $"{outputFile.FullName}.{backupFileNo}.bak", true);
            }

            File.WriteAllText(outputFile.FullName, code);
            AnsiConsole.MarkupLine($"[green] The parser '{outputFile.FullName}' is successfully created![/]");
        }
    }
    return new OutputResult(visitResult.Successful && outputResult.Successful, visitResult.Messages, outputResult.Messages);
}

static OutputResult CreateAST(FileInfo grammarFile, FileInfo codeFile, FileInfo outputFile, String overrideOption)
{
    List<ParserMessage> visitorMessages = new List<ParserMessage>();
    List<ParserMessage> outputMessages = new List<ParserMessage>();

    ParseidonParser parser = new ParseidonParser();
    Parseidon.Parser.ParseResult grammarParseResult = parser.Parse(File.ReadAllText(grammarFile.FullName));
    visitorMessages.AddRange(grammarParseResult.Messages);
    if (!grammarParseResult.Successful)
        return new OutputResult(false, visitorMessages, outputMessages);

    ParseidonVisitor visitor = new ParseidonVisitor();
    IVisitResult visitResult = grammarParseResult.Visit(visitor);
    visitorMessages.AddRange(visitResult.Messages);
    if (!visitResult.Successful || visitResult is not ParseidonVisitor.IGetResults typedVisitResult)
        return new OutputResult(false, visitorMessages, outputMessages);

    String runtimeSuffix = Guid.NewGuid().ToString("N");
    String runtimeNamespace = $"Parseidon.RuntimeParser_{runtimeSuffix}";
    String runtimeClass = $"RuntimeParser_{runtimeSuffix}";
    Grammar.CreateOutputResult parserCodeResult = typedVisitResult.GetParserCode(runtimeNamespace, runtimeClass, false);
    outputMessages.AddRange(parserCodeResult.Messages);
    if (!parserCodeResult.Successful)
        return new OutputResult(false, visitorMessages, outputMessages);

    Assembly? runtimeParserAssembly = CompileRuntimeParser(parserCodeResult.Output, outputMessages);
    if (runtimeParserAssembly is null)
        return new OutputResult(false, visitorMessages, outputMessages);

    Boolean parseSuccessful = TryParseCodeWithRuntimeParser(runtimeParserAssembly, File.ReadAllText(codeFile.FullName), outputMessages, out String? astText);
    if (!parseSuccessful || astText is null)
        return new OutputResult(false, visitorMessages, outputMessages);

    WriteOutputWithBackup(outputFile, astText, overrideOption);
    return new OutputResult(true, visitorMessages, outputMessages);
}

static OutputResult CreateTextMateGrammar(Parseidon.Parser.ParseResult parseResult, FileInfo outputFile, String overrideOption)
{
    ParseidonVisitor visitor = new ParseidonVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);
    Grammar.CreateOutputResult outputResult = Grammar.CreateOutputResult.Empty;
    if (visitResult.Successful && visitResult is ParseidonVisitor.IGetResults typedVisitResult)
    {
        outputResult = typedVisitResult.GetTextMateGrammar();
        if (outputResult.Successful)
        {
            File.WriteAllText(outputFile.FullName, outputResult.Output);
            AnsiConsole.MarkupLine($"[green] The TextMate grammar '{outputFile.FullName}' is successfully created![/]");
        }
    }
    return new OutputResult(visitResult.Successful && outputResult.Successful, visitResult.Messages, outputResult.Messages);
}

static OutputResult CreateVSCodePackage(Parseidon.Parser.ParseResult parseResult, DirectoryInfo outputFolder, String overrideOption, FileInfo grammarFile, String? versionOverride = null, FileInfo? packageJsonOverride = null)
{
    ParseidonVisitor visitor = new ParseidonVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);
    Boolean successful = false;
    List<ParserMessage> outputMessages = new List<ParserMessage>();
    if (visitResult.Successful && visitResult is ParseidonVisitor.IGetResults typedVisitResult)
    {
        var textmateGrammarResult = typedVisitResult.GetTextMateGrammar();
        outputMessages = outputMessages.Concat(textmateGrammarResult.Messages).ToList();
        if (textmateGrammarResult.Successful)
        {
            var languageResult = typedVisitResult.GetLanguageConfig();
            outputMessages = outputMessages.Concat(languageResult.Messages).ToList();
            if (languageResult.Successful)
            {
                String LoadMergeContent(String mergePath)
                {
                    FileInfo mergeFile;
                    if (Path.IsPathRooted(mergePath))
                    {
                        mergeFile = new FileInfo(mergePath);
                    }
                    else
                    {
                        if (grammarFile.Directory == null)
                            throw new InvalidOperationException($"Cannot resolve relative path '{mergePath}' because the grammar file's directory is null.");
                        mergeFile = new FileInfo(Path.GetFullPath(Path.Combine(grammarFile.Directory.FullName, mergePath)));
                    }
                    mergeFile.Refresh();
                    if (!mergeFile.Exists)
                        throw new FileNotFoundException($"The file '{mergeFile.FullName}' could not be found!");
                    return File.ReadAllText(mergeFile.FullName);
                }

                var vscodePackageResult = typedVisitResult.GetVSCodePackage(versionOverride, LoadMergeContent, packageJsonOverride?.FullName);
                outputMessages = outputMessages.Concat(vscodePackageResult.Messages).ToList();
                if (vscodePackageResult.Successful)
                {
                    if (outputFolder.Exists)
                        outputFolder.Delete(true);
                    outputFolder.Create();
                    new DirectoryInfo(Path.Combine(outputFolder.FullName, "syntaxes")).Create();
                    File.WriteAllText(Path.Combine(outputFolder.FullName, $"language-configuration.json"), languageResult.Output);
                    File.WriteAllText(Path.Combine(outputFolder.FullName, $"package.json"), vscodePackageResult.Output);
                    File.WriteAllText(Path.Combine(outputFolder.FullName, $"syntaxes/parseidon.tmLanguage.json"), textmateGrammarResult.Output);
                    AnsiConsole.MarkupLine($"[green] The VS Code package in '{outputFolder.FullName}' is successfully created![/]");
                    successful = true;
                }
            }
        }
    }
    return new OutputResult(successful, visitResult.Messages, outputMessages);
}

static Assembly? CompileRuntimeParser(String parserCode, IList<ParserMessage> messages)
{
    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(parserCode);
    ImmutableArray<MetadataReference> references = GetTrustedPlatformReferences();
    CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: $"ParseidonRuntimeParser_{Guid.NewGuid():N}",
        syntaxTrees: new[] { syntaxTree },
        references: references,
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

    using MemoryStream ms = new MemoryStream();
    Microsoft.CodeAnalysis.Emit.EmitResult emitResult = compilation.Emit(ms);
    foreach (var diagnostic in emitResult.Diagnostics.Where(d => d.Severity != DiagnosticSeverity.Hidden))
    {
        var location = diagnostic.Location.GetLineSpan();
        UInt32 row = (UInt32)Math.Max(location.StartLinePosition.Line + 1, 0);
        UInt32 column = (UInt32)Math.Max(location.StartLinePosition.Character + 1, 0);
        ParserMessage.MessageType messageType = diagnostic.Severity == DiagnosticSeverity.Error ? ParserMessage.MessageType.Error : ParserMessage.MessageType.Warning;
        messages.Add(new ParserMessage(diagnostic.ToString(), messageType, (row, column)));
    }

    if (!emitResult.Success)
        return null;

    ms.Position = 0;
    return Assembly.Load(ms.ToArray());
}

static ImmutableArray<MetadataReference> GetTrustedPlatformReferences()
{
    String? tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as String;
    if (String.IsNullOrWhiteSpace(tpa))
        throw new InvalidOperationException("Could not locate trusted platform assemblies for compilation.");

    return tpa
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToImmutableArray();
}

static Boolean TryParseCodeWithRuntimeParser(Assembly assembly, String code, IList<ParserMessage> messages, out String? astText)
{
    astText = null;
    Type? parserType = assembly.GetTypes().FirstOrDefault(t => t.GetMethod("Parse", new[] { typeof(String) }) is not null);
    if (parserType is null)
    {
        messages.Add(new ParserMessage("No parser type with a Parse(string) method could be found in the generated assembly.", ParserMessage.MessageType.Error, (0u, 0u)));
        return false;
    }

    Object? parserInstance = Activator.CreateInstance(parserType);
    if (parserInstance is null)
    {
        messages.Add(new ParserMessage("Failed to create an instance of the generated parser.", ParserMessage.MessageType.Error, (0u, 0u)));
        return false;
    }

    MethodInfo? parseMethod = parserType.GetMethod("Parse", new[] { typeof(String) });
    Object? parseResult = parseMethod?.Invoke(parserInstance, new Object[] { code });
    if (parseResult is null)
    {
        messages.Add(new ParserMessage("The generated parser returned no result.", ParserMessage.MessageType.Error, (0u, 0u)));
        return false;
    }

    Type parseResultType = parseResult.GetType();
    PropertyInfo? successfulProp = parseResultType.GetProperty("Successful");
    PropertyInfo? messagesProp = parseResultType.GetProperty("Messages");
    PropertyInfo? rootNodeProp = parseResultType.GetProperty("RootNode");

    if (messagesProp?.GetValue(parseResult) is IEnumerable<Object> runtimeMessages)
    {
        foreach (ParserMessage message in ConvertRuntimeMessages(runtimeMessages))
            messages.Add(message);
    }

    Boolean successful = successfulProp?.GetValue(parseResult) as Boolean? ?? false;
    if (!successful)
        return false;

    Object? rootNode = rootNodeProp?.GetValue(parseResult);
    if (rootNode is null)
    {
        messages.Add(new ParserMessage("The generated parser did not return an AST root node.", ParserMessage.MessageType.Error, (0u, 0u)));
        return false;
    }

    astText = RenderAstWithReflection(rootNode);
    return true;
}

static IEnumerable<ParserMessage> ConvertRuntimeMessages(IEnumerable<Object> runtimeMessages)
{
    foreach (Object message in runtimeMessages)
    {
        Type messageType = message.GetType();
        String text = messageType.GetProperty("Message")?.GetValue(message)?.ToString() ?? "";
        UInt32 row = messageType.GetProperty("Row")?.GetValue(message) as UInt32? ?? 0u;
        UInt32 column = messageType.GetProperty("Column")?.GetValue(message) as UInt32? ?? 0u;
        Object? typeValue = messageType.GetProperty("Type")?.GetValue(message);
        ParserMessage.MessageType mappedType = ParserMessage.MessageType.Warning;
        if (typeValue?.ToString()?.Equals("Error", StringComparison.OrdinalIgnoreCase) ?? false)
            mappedType = ParserMessage.MessageType.Error;
        yield return new ParserMessage(text, mappedType, (row, column));
    }
}

static String RenderAstWithReflection(Object rootNode)
{
    StringBuilder stringBuilder = new StringBuilder();

    static void PrintNode(Object node, bool[] crossings, StringBuilder stringBuilder)
    {
        Type nodeType = node.GetType();
        String name = nodeType.GetProperty("Name")?.GetValue(node)?.ToString() ?? "?";
        Int32 tokenId = nodeType.GetProperty("TokenId")?.GetValue(node) as Int32? ?? 0;
        Int32 position = nodeType.GetProperty("Position")?.GetValue(node) as Int32? ?? 0;
        String text = nodeType.GetProperty("Text")?.GetValue(node)?.ToString() ?? String.Empty;
        IEnumerable<Object> children = nodeType.GetProperty("Children")?.GetValue(node) as IEnumerable<Object> ?? Enumerable.Empty<Object>();

        for (int i = 0; i < crossings.Length - 1; i++)
            stringBuilder.Append(crossings[i] ? "  " : "  ");
        if (crossings.Length > 0)
            stringBuilder.Append("- ");

        stringBuilder.Append($"{name}[{tokenId}] ({position}): ");
        if (!String.IsNullOrEmpty(text))
            stringBuilder.Append(text.FormatLiteral(true));
        stringBuilder.AppendLine();

        Object[] childArray = children.ToArray();
        for (int i = 0; i < childArray.Length; i++)
        {
            bool[] childCrossings = new bool[crossings.Length + 1];
            Array.Copy(crossings, childCrossings, crossings.Length);
            childCrossings[childCrossings.Length - 1] = (i < childArray.Length - 1);
            PrintNode(childArray[i], childCrossings, stringBuilder);
        }
    }

    PrintNode(rootNode, Array.Empty<bool>(), stringBuilder);
    return stringBuilder.ToString();
}

static Int32 ValidateFileInput(FileInfo grammarFile, FileInfo outputFile, String overrideOption)
{
    if (!grammarFile.Exists)
    {
        PrintMessage(ParserMessage.MessageType.Error, $"The file '{grammarFile.FullName}' could not be found!");
        return 1;
    }
    if (outputFile.Exists)
    {
        if (overrideOption.Equals("abort"))
        {
            PrintMessage(ParserMessage.MessageType.Error, $"The file '{outputFile.FullName}' already exists!");
            return 1;
        }
        if (overrideOption.Equals("ask"))
        {
            PrintMessage(ParserMessage.MessageType.Error, $"The file '{outputFile.FullName}' already exists!");
            if (!AnsiConsole.Prompt(new ConfirmationPrompt("Should it be overwritten?")))
                return 1;
        }
    }
    return 0;
}

static Int32 ValidateFolderInput(FileInfo grammarFile, DirectoryInfo outputFolder, String overrideOption)
{
    if (!grammarFile.Exists)
    {
        PrintMessage(ParserMessage.MessageType.Error, $"The file '{grammarFile.FullName}' could not be found!");
        return 1;
    }
    if (outputFolder.Exists)
    {
        if (overrideOption.Equals("abort"))
        {
            PrintMessage(ParserMessage.MessageType.Error, $"The file '{outputFolder.FullName}' already exists!");
            return 1;
        }
        if (overrideOption.Equals("ask"))
        {
            PrintMessage(ParserMessage.MessageType.Error, $"The file '{outputFolder.FullName}' already exists!");
            if (!AnsiConsole.Prompt(new ConfirmationPrompt("Should it be overwritten?")))
                return 1;
        }
    }
    return 0;
}

static Int32 ValidateAstInput(FileInfo grammarFile, FileInfo codeFile, FileInfo outputFile, String overrideOption)
{
    Int32 exitCode = ValidateFileInput(grammarFile, outputFile, overrideOption);
    if (exitCode != 0)
        return exitCode;

    if (!codeFile.Exists)
    {
        PrintMessage(ParserMessage.MessageType.Error, $"The file '{codeFile.FullName}' could not be found!");
        return 1;
    }

    return 0;
}

static void WriteOutputWithBackup(FileInfo outputFile, String content, String overrideOption)
{
    if (outputFile.Exists && overrideOption.Equals("backup"))
    {
        Int32 backupFileNo = 1;
        while (File.Exists($"{outputFile.FullName}.{backupFileNo}.bak"))
            backupFileNo++;
        File.Copy(outputFile.FullName, $"{outputFile.FullName}.{backupFileNo}.bak", true);
    }

    File.WriteAllText(outputFile.FullName, content);
}

internal sealed class OutputResult
{
    public OutputResult(Boolean successful, IReadOnlyList<ParserMessage> visitorMessages, IReadOnlyList<ParserMessage> createOutputMessages)
    {
        Successful = successful;
        VisitorMessages = visitorMessages;
        CreateOutputMessages = createOutputMessages;
    }
    internal Boolean Successful { get; }
    internal IReadOnlyList<ParserMessage> VisitorMessages { get; }
    internal IReadOnlyList<ParserMessage> CreateOutputMessages { get; }
}
