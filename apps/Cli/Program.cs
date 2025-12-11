using Parseidon.Parser;
using Spectre.Console;
using System.CommandLine;
using Parseidon.Cli;
using Parseidon.Parser.Grammar;

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

var astCommand = new Command("ast")
{
    Description = "Create the AST (Abstract Syntax Tree) for the grammar as a YAML file"
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

var grammarFileArgument = new Argument<FileInfo>(name: "GRAMMAR-FILE")
{
    Description = "The grammar file to be used"
};
grammarFileArgument.AcceptLegalFilePathsOnly();
parseCommand.Add(grammarFileArgument);
astCommand.Add(grammarFileArgument);
textMateCommand.Add(grammarFileArgument);
vsCodeCommand.Add(grammarFileArgument);

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
        return RunParser(
            grammarFile,
            () => ValidateFileInput(grammarFile, outputFile, doOverride),
            (result) => CreateParser(result, outputFile, doOverride)
        );
    }
);

astCommand.SetAction((parseResult) =>
    {
        FileInfo grammarFile = parseResult.GetValue(grammarFileArgument)!;
        FileInfo outputFile = parseResult.GetValue(outputFileArgument)!;
        String doOverride = parseResult.GetValue<String>(overrideOption)!;
        return RunParser(
            grammarFile,
            () => ValidateFileInput(grammarFile, outputFile, doOverride),
            (result) => CreateAST(result, outputFile, doOverride)
        );
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
        return RunParser(
            grammarFile,
            () => ValidateFolderInput(grammarFile, outputFolder, doOverride),
            (result) => CreateVSCodePackage(result, outputFolder, doOverride)
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

static OutputResult CreateParser(Parseidon.Parser.ParseResult parseResult, FileInfo outputFile, String overrideOption)
{
    IVisitor visitor = new ParseidonVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);

    Grammar.CreateOutputResult outputResult = Grammar.CreateOutputResult.Empty;

    if (visitResult.Successful && visitResult is ParseidonVisitor.IGetResults typedVisitResult)
    {
        outputResult = typedVisitResult.ParserCode;
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

static OutputResult CreateAST(Parseidon.Parser.ParseResult parseResult, FileInfo outputFile, String overrideOption)
{
    RenderASTVisitor visitor = new RenderASTVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);
    Grammar.CreateOutputResult outputResult = Grammar.CreateOutputResult.Empty;
    if (visitResult.Successful && visitResult is RenderASTVisitor.IGetAST typedVisitResult)
    {
        outputResult = typedVisitResult.AST;
        if (outputResult.Successful)
        {
            File.WriteAllText(outputFile.FullName, outputResult.Output);
            AnsiConsole.MarkupLine($"[green] The AST-file '{outputFile.FullName}' is successfully created![/]");
        }
    }
    return new OutputResult(visitResult.Successful && outputResult.Successful, visitResult.Messages, outputResult.Messages);
}

static OutputResult CreateTextMateGrammar(Parseidon.Parser.ParseResult parseResult, FileInfo outputFile, String overrideOption)
{
    ParseidonVisitor visitor = new ParseidonVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);
    Grammar.CreateOutputResult outputResult = Grammar.CreateOutputResult.Empty;
    if (visitResult.Successful && visitResult is ParseidonVisitor.IGetResults typedVisitResult)
    {
        outputResult = typedVisitResult.TextMateGrammar;
        if (outputResult.Successful)
        {
            File.WriteAllText(outputFile.FullName, outputResult.Output);
            AnsiConsole.MarkupLine($"[green] The TextMate grammar '{outputFile.FullName}' is successfully created![/]");
        }
    }
    return new OutputResult(visitResult.Successful && outputResult.Successful, visitResult.Messages, outputResult.Messages);
}

static OutputResult CreateVSCodePackage(Parseidon.Parser.ParseResult parseResult, DirectoryInfo outputFolder, String overrideOption)
{
    ParseidonVisitor visitor = new ParseidonVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);
    Boolean successful = false;
    List<ParserMessage> outputMessages = new List<ParserMessage>();
    if (visitResult.Successful && visitResult is ParseidonVisitor.IGetResults typedVisitResult)
    {
        var textmateGrammarResult = typedVisitResult.TextMateGrammar;
        outputMessages = outputMessages.Concat(textmateGrammarResult.Messages).ToList();
        if (textmateGrammarResult.Successful)
        {
            var languageResult = typedVisitResult.LanguageConfig;
            outputMessages = outputMessages.Concat(languageResult.Messages).ToList();
            if (languageResult.Successful)
            {
                var vscodePackageResult = typedVisitResult.VSCodePackage;
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

internal class OutputResult
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
