using Parseidon.Parser;
using Spectre.Console;
using System.CommandLine;
using Parseidon.Cli;

var rootCommand = new RootCommand("Parser generator for .NET");

var overrideOption = new Option<string>(name: "--override", description: "How to handle if the output file already exists", getDefaultValue: () => "ask")
    .FromAmong("ask", "abort", "backup", "override");
overrideOption.AddAlias("-o");

rootCommand.AddGlobalOption(overrideOption);

var parseCommand = new Command("parser", "Create parser class for the grammar as a C# class");

var namespaceOption = new Option<String>(name: "--namespace", "The namespace for the generated C# class");
namespaceOption.AddAlias("-n");
parseCommand.Add(namespaceOption);

var classnameOption = new Option<String>(name: "--classname", "The class name for the generated C# class");
classnameOption.AddAlias("-c");
parseCommand.Add(classnameOption);

rootCommand.Add(parseCommand);

var astCommand = new Command("ast", "Create the AST (Abstract Syntax Tree) for the grammar as a YAML file");
rootCommand.Add(astCommand);

var textMateCommand = new Command("textmate", "Create a TextMate grammar from the provided grammar definition");
rootCommand.Add(textMateCommand);

var vsCodeCommand = new Command("vscode", "Create a VS Code extension with the syntax highlighting from the provided grammar definition");
rootCommand.Add(vsCodeCommand);

var grammarFileArgument = new Argument<FileInfo>(name: "GRAMMAR-FILE", description: "The grammar file to be used");
parseCommand.Add(grammarFileArgument);
astCommand.Add(grammarFileArgument);
textMateCommand.Add(grammarFileArgument);
vsCodeCommand.Add(grammarFileArgument);

var outputFileArgument = new Argument<FileInfo>(name: "OUTPUT-FILE", description: "The output file");
parseCommand.Add(outputFileArgument);
astCommand.Add(outputFileArgument);
textMateCommand.Add(outputFileArgument);

var outputFolderArgument = new Argument<DirectoryInfo>(name: "OUTPUT-FOLDER", description: "The output folder");
vsCodeCommand.Add(outputFolderArgument);

parseCommand.SetHandler(
    (grammarFile, outputFile, overrideOption, parserNamespace, parserClassname) =>
    {
        int exitCode = RunParser(
            grammarFile,
            () => ValidateFileInput(grammarFile, outputFile, overrideOption),
            overrideOption,
            (result) => CreateParser(result, outputFile, overrideOption, parserNamespace, parserClassname)
        );
        Environment.Exit(exitCode);
    },
    grammarFileArgument, outputFileArgument, overrideOption, namespaceOption, classnameOption);

astCommand.SetHandler(
    (grammarFile, outputFile, overrideOption) =>
    {
        int exitCode = RunParser(
            grammarFile,
            () => ValidateFileInput(grammarFile, outputFile, overrideOption),
            overrideOption,
            (result) => CreateAST(result, outputFile, overrideOption)
        );
        Environment.Exit(exitCode);
    },
    grammarFileArgument, outputFileArgument, overrideOption);

textMateCommand.SetHandler(
    (grammarFile, outputFile, overrideOption) =>
    {
        int exitCode = RunParser(
            grammarFile,
            () => ValidateFileInput(grammarFile, outputFile, overrideOption),
            overrideOption,
            (result) => CreateTextMateGrammar(result, outputFile, overrideOption)
        );
        Environment.Exit(exitCode);
    },
    grammarFileArgument, outputFileArgument, overrideOption);

vsCodeCommand.SetHandler(
    (grammarFile, outputFolder, overrideOption) =>
    {
        int exitCode = RunParser(
            grammarFile,
            () => ValidateFolderInput(grammarFile, outputFolder, overrideOption),
            overrideOption,
            (result) => CreateVSCodePackage(result, outputFolder, overrideOption)
        );
        Environment.Exit(exitCode);
    },
    grammarFileArgument, outputFolderArgument, overrideOption);

return await rootCommand.InvokeAsync(args);

static int RunParser(FileInfo grammarFile, Func<Int32> validateInput, String overrideOption, Func<ParseResult, IVisitResult> processResult)
{
    // int exitCode = ValidateFileInput(grammarFile, outputFile, overrideOption);
    Int32 exitCode = validateInput();
    if (exitCode != 0)
        return exitCode;

    ParseidonParser Parser = new ParseidonParser();
    ParseResult parseResult = Parser.Parse(File.ReadAllText(grammarFile.FullName));
    IVisitResult? visitResult = null;
    if (parseResult.Successful)
        visitResult = processResult(parseResult);

    int visitResultExitCode = ProcessMessages(visitResult?.Messages);
    int parseResultExitCode = ProcessMessages(parseResult.Messages);

    // Exit Code 1 wenn einer der beiden Fehler hatte
    if (visitResultExitCode != 0 || parseResultExitCode != 0)
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

static IVisitResult CreateParser(ParseResult parseResult, FileInfo outputFile, String overrideOption, String parserNamespace, String parserClassname)
{
    IVisitor visitor = new CreateCodeVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);

    if (visitResult.Successful && visitResult is CreateCodeVisitor.IGetCode)
    {
        String code = (visitResult as CreateCodeVisitor.IGetCode)!.Code ?? "";
        code =
            $"""
        //****************************************//
        //*                                      *//
        //* This code is generated by parseidon. *//
        //*     https://github.com/parseidon     *//
        //*                                      *//
        //*         {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}          *//
        //*                                      *//
        //****************************************//

        {code}
        """;
        if (outputFile.Exists && overrideOption.Equals("backup"))
        {
            Int32 backupFileNo = 1;
            while (File.Exists($"{outputFile.FullName}.{backupFileNo}.bak"))
                backupFileNo++;
            File.Copy(outputFile.FullName, $"{outputFile.FullName}.{backupFileNo}.bak", true);
        }

        File.WriteAllText(outputFile.FullName, code);
        AnsiConsole.MarkupLine($"[green] The parser '{outputFile.FullName}' is sucessfully created![/]");
    }
    return visitResult;
}

static IVisitResult CreateAST(ParseResult parseResult, FileInfo outputFile, String overrideOption)
{
    RenderASTVisitor visitor = new RenderASTVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);
    if (visitResult.Successful && visitResult is RenderASTVisitor.IGetAST)
    {
        File.WriteAllText(outputFile.FullName, (visitResult as RenderASTVisitor.IGetAST)!.AST ?? "");
        AnsiConsole.MarkupLine($"[green] The AST-file '{outputFile.FullName}' is sucessfully created![/]");
    }
    return visitResult;
}

static IVisitResult CreateTextMateGrammar(ParseResult parseResult, FileInfo outputFile, String overrideOption)
{
    TextMateGrammarVisitor visitor = new TextMateGrammarVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);
    if (visitResult.Successful && visitResult is TextMateGrammarVisitor.IGetTextMateGrammar grammarResult)
    {
        File.WriteAllText(outputFile.FullName, grammarResult.GrammarJson ?? "");
        AnsiConsole.MarkupLine($"[green] The TextMate grammar '{outputFile.FullName}' is sucessfully created![/]");
    }
    return visitResult;
}

static IVisitResult CreateVSCodePackage(ParseResult parseResult, DirectoryInfo outputFolder, String overrideOption)
{
    TextMateGrammarVisitor visitor = new TextMateGrammarVisitor();
    IVisitResult visitResult = parseResult.Visit(visitor);
    if (visitResult.Successful && visitResult is TextMateGrammarVisitor.IGetTextMateGrammar grammarResult)
    {
        if (outputFolder.Exists)
            outputFolder.Delete(true);
        outputFolder.Create();
        (new DirectoryInfo(Path.Combine(outputFolder.FullName, "syntaxes"))).Create();
        File.WriteAllText(Path.Combine(outputFolder.FullName, $"language-configuration.json"), grammarResult.LanguageConfigJson);
        File.WriteAllText(Path.Combine(outputFolder.FullName, $"package.json"), grammarResult.PackageJson);
        File.WriteAllText(Path.Combine(outputFolder.FullName, $"syntaxes/parseidon.tmLanguage.json"), grammarResult.GrammarJson);
        AnsiConsole.MarkupLine($"[green] The VS Code package in '{outputFolder.FullName}' is sucessfully created![/]");
    }
    return visitResult;
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
