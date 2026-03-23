using Pop.Language;
using Pop.Runtime;

var sourceFile = args.Length > 0
    ? LoadSourceFile(args)
    : SourceFile.FromText(ReadInput());

var model = SemanticModel.Create(sourceFile);

if (model.Diagnostics.Count > 0)
{
    foreach (var diagnostic in model.Diagnostics)
    {
        Console.WriteLine(diagnostic);
    }

    return;
}

var runtime = new ScriptRuntime(Console.Out, Console.In);
var result = runtime.Execute(model);

if (!result.Succeeded && result.Diagnostics.Count > 0)
{
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.WriteLine(diagnostic);
    }
}

static string ReadInput()
{
    Console.WriteLine("Enter script lines. Type ':run' on its own line to execute.");

    var lines = new List<string>();
    while (true)
    {
        Console.Write(lines.Count == 0 ? "input> " : ".....> ");
        var line = Console.ReadLine();
        if (line is null || line == ":run")
        {
            break;
        }

        lines.Add(line);
    }

    return string.Join(Environment.NewLine, lines);
}

static SourceFile LoadSourceFile(string[] args)
{
    if (args.Length == 1 && File.Exists(args[0]))
    {
        return SourceFile.Load(new FileInfo(args[0]));
    }

    return SourceFile.FromText(string.Join(" ", args));
}
