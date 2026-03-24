using System.Collections.Concurrent;
using System.Threading;

namespace Pop.Language;

public static class ModuleLoader
{
    private static readonly ConcurrentDictionary<string, CachedModule> Cache = new(GetPathComparer());
    private static readonly AsyncLocal<HashSet<string>?> LoadingPaths = new();

    public static SemanticModel LoadSemanticModel(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var normalizedPath = Path.GetFullPath(path);
        var fileInfo = new FileInfo(normalizedPath);
        var lastWriteTimeUtc = fileInfo.LastWriteTimeUtc;

        if (Cache.TryGetValue(normalizedPath, out var cachedModule) &&
            cachedModule.LastWriteTimeUtc == lastWriteTimeUtc)
        {
            return cachedModule.Model;
        }

        var loadingPaths = LoadingPaths.Value;
        if (loadingPaths is null)
        {
            loadingPaths = new HashSet<string>(GetPathComparer());
            LoadingPaths.Value = loadingPaths;
        }

        if (!loadingPaths.Add(normalizedPath))
        {
            throw new InvalidOperationException($"Cyclic inject detected for '{normalizedPath}'.");
        }

        try
        {
            var model = SemanticModel.Create(SourceFile.Load(fileInfo));
            Cache[normalizedPath] = new CachedModule(lastWriteTimeUtc, model);
            return model;
        }
        finally
        {
            loadingPaths.Remove(normalizedPath);
            if (loadingPaths.Count == 0)
            {
                LoadingPaths.Value = null;
            }
        }
    }

    private static StringComparer GetPathComparer()
    {
        return OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }

    private sealed record CachedModule(DateTime LastWriteTimeUtc, SemanticModel Model);
}
