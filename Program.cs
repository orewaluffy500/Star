using star;
using star.interp;

string option = args.Length > 0 ? args[0] : "";

if (option == "-v")
{
    Console.WriteLine("Beta 1");
}

else if (option == "-run")
{
    expectArgs(2);
    string file = args[1];
    runFile(file);
}

void expectArgs(int argc)
{
    if (args.Length < argc)
    {
        Console.Error.WriteLine($"expected {argc} argument(s)");
        Environment.Exit(1);
    }
}

void runFile(string fileName)
{
    if (!File.Exists(fileName))
    {
        Console.Error.WriteLine($"file does not exist!");
        Environment.Exit(1);
    }

    string source = File.ReadAllText(fileName);

    Lexer lexer = new(source, fileName);
    var (tokens, error) = lexer.Tokenize();

    if (error is not null)
    {
        Console.Error.WriteLine(error.Prettify());
        Environment.Exit(2);
    }

    if (args.Contains("-Dtok")) Console.WriteLine(string.Join(' ', tokens));

    // PARSER

    Parser parser = new(fileName, tokens);
    parser.Next();

    var result = parser.PlainBody();
    if (result.HasError()){ Console.Error.WriteLine(result.Error?.Prettify()); return; }
    else if (args.Contains("-Dast")) Console.WriteLine(result.Node);

    // INTEPRRETER

    Interpreter interpreter = new();
    RTResult rtResult = interpreter.Visit(result.Node!);

    if (rtResult.HasError()) Console.Error.WriteLine(rtResult.Error?.Prettify());
}