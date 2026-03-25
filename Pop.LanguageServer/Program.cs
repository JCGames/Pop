using System.Text;
using System.Text.Json;
using Pop.Language;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

string? line;
while ((line = Console.ReadLine()) is not null)
{
    if (string.IsNullOrWhiteSpace(line))
    {
        continue;
    }

    ServerRequest? request;
    try
    {
        request = JsonSerializer.Deserialize<ServerRequest>(line);
    }
    catch
    {
        continue;
    }

    if (request is null || !string.Equals(request.Type, "analyze", StringComparison.Ordinal))
    {
        continue;
    }

    var response = Analyze(request);
    Console.WriteLine(JsonSerializer.Serialize(response));
}

static ServerResponse Analyze(ServerRequest request)
{
    try
    {
        var filePath = string.IsNullOrWhiteSpace(request.Path)
            ? Path.Combine(Directory.GetCurrentDirectory(), "untitled.pop")
            : request.Path;

        var sourceFile = SourceFile.FromText(request.Text ?? string.Empty, new FileInfo(filePath));
        var model = SemanticModel.Create(sourceFile);

        return new ServerResponse(
            request.Id,
            model.Diagnostics.Select(static diagnostic => new ServerDiagnostic(
                diagnostic.Message,
                diagnostic.Level.ToString(),
                diagnostic.Location.Line,
                diagnostic.Location.Column,
                Math.Max(1, diagnostic.Location.Span.Length)))
                .ToArray());
    }
    catch (Exception exception)
    {
        return new ServerResponse(
            request.Id,
            [
                new ServerDiagnostic(
                    exception.Message,
                    DiagnosticLevel.Error.ToString(),
                    1,
                    1,
                    1)
            ]);
    }
}

internal sealed record ServerRequest(
    string Type,
    int Id,
    string? Path,
    string? Text);

internal sealed record ServerResponse(
    int Id,
    IReadOnlyList<ServerDiagnostic> Diagnostics);

internal sealed record ServerDiagnostic(
    string Message,
    string Level,
    int Line,
    int Column,
    int Length);
