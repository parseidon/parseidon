using Parseidon.SourceGenExample.Generated;

namespace Parseidon.SourceGenExample;

/// <summary>
/// Example application demonstrating the Parseidon.SourceGen source generator.
/// The CalculatorParser class is automatically generated at compile time from Calculator.pgram.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Parseidon Source Generator Example ===");
        Console.WriteLine();
        Console.WriteLine("This example demonstrates a calculator parser generated from Calculator.pgram");
        Console.WriteLine();

        // Create an instance of the generated parser
        var parser = new CalculatorParser();

        // Test expressions
        string[] testExpressions = new[]
        {
            "2 + 3",
            "10 - 4",
            "5 * 6",
            "20 / 4",
            "2 + 3 * 4",
            "(2 + 3) * 4",
            "10 + 20 - 5 * 2",
            "invalid expression!"
        };

        foreach (var expression in testExpressions)
        {
            Console.WriteLine($"Parsing: {expression}");
            var result = parser.Parse(expression);

            if (result.Successful)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Parse successful!");
                Console.ResetColor();
                Console.WriteLine($"  Root node: {result.RootNode?.Name}");
                Console.WriteLine($"  Child nodes: {result.RootNode?.Children.Count}");
                
                // Display the AST structure
                if (result.RootNode != null)
                {
                    Console.WriteLine("  AST Structure:");
                    PrintASTNode(result.RootNode, "    ");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Parse failed!");
                Console.ResetColor();
                foreach (var message in result.Messages)
                {
                    Console.WriteLine($"  {message.Type}: {message.Message} at line {message.Row}, column {message.Collumn}");
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static void PrintASTNode(CalculatorParser.ASTNode node, string indent, int maxDepth = 3)
    {
        if (maxDepth <= 0) return;

        Console.WriteLine($"{indent}{node.Name} [{node.TokenId}]");
        
        if (!string.IsNullOrEmpty(node.Text) && node.Children.Count == 0)
        {
            Console.WriteLine($"{indent}  Text: \"{node.Text}\"");
        }

        foreach (var child in node.Children.Take(5)) // Limit to first 5 children
        {
            PrintASTNode(child, indent + "  ", maxDepth - 1);
        }

        if (node.Children.Count > 5)
        {
            Console.WriteLine($"{indent}  ... ({node.Children.Count - 5} more children)");
        }
    }
}
