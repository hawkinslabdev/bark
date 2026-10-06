using Bark.Models;

namespace Bark.Services.Api;

/// <summary>Result of resolving an <c>api:</c> front matter value.</summary>
public sealed record ApiPageModel(string SpecKey, ApiOperation? Operation, ApiObject? Object);

/// <summary>
/// Resolves <c>api:</c> front matter to an operation or object from a spec file inside the docs folder.
/// Spec files are read from disk only: Bark never fetches a spec or calls an API server-side.
/// </summary>
public sealed class ApiSpecLoader(string docsPath)
{
    public static readonly string[] OpenApiExtensions = [".yaml", ".yml", ".json"];
    public static readonly string[] GraphQlExtensions = [".graphql", ".graphqls", ".gql"];
    public static readonly string[] ODataExtensions = [".xml", ".edmx", ".csdl"];

    private readonly string _root = Path.GetFullPath(docsPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    private readonly Dictionary<string, object> _readers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _contents = new(StringComparer.Ordinal);

    /// <summary>Spec files read during this build, keyed by docs-relative path, with their contents.</summary>
    public IReadOnlyDictionary<string, string> Contents => _contents;

    /// <exception cref="ApiSpecException">The value, file or selector is invalid.</exception>
    public ApiPageModel Load(string api, string pageRelativePath)
    {
        var parts = api.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            throw new ApiSpecException($"Expected '<file> <selector> <name>', found '{api}'.");

        var (key, reader) = Reader(parts[0], pageRelativePath);
        var selector = parts[1];
        var name = string.Join(' ', parts[2..]);
        var isSchema = selector.Equals("schema", StringComparison.OrdinalIgnoreCase);

        ApiOperation? operation = null;
        ApiObject? obj = null;
        switch (reader)
        {
            case OpenApiReader openApi when isSchema:
                obj = openApi.GetSchema(name);
                break;
            case OpenApiReader openApi when selector.Equals("webhook", StringComparison.OrdinalIgnoreCase):
                operation = openApi.GetWebhook(name);
                break;
            case OpenApiReader openApi:
                operation = openApi.GetOperation(selector, name);
                break;
            case GraphQlReader graphQl when isSchema:
                obj = graphQl.GetSchema(name);
                break;
            case GraphQlReader graphQl:
                operation = graphQl.GetOperation(selector.ToLowerInvariant(), name);
                break;
            case ODataReader oData when isSchema:
                obj = oData.GetSchema(name);
                break;
            case ODataReader oData:
                operation = oData.GetOperation(selector, name);
                break;
        }

        if (operation is null && obj is null)
            throw new ApiSpecException($"'{selector} {name}' is not defined in {key}.");
        return new ApiPageModel(key, operation, obj);
    }

    /// <summary>Every operation and object in a spec file, in sidebar order.</summary>
    /// <exception cref="ApiSpecException">The file is missing, outside the docs folder or invalid.</exception>
    public (string SpecKey, IReadOnlyList<ApiCatalogEntry> Entries) Catalog(string file, string pageRelativePath)
    {
        var (key, reader) = Reader(file.Trim(), pageRelativePath);
        IReadOnlyList<ApiCatalogEntry> entries = reader switch
        {
            OpenApiReader openApi => openApi.Catalog(),
            GraphQlReader graphQl => graphQl.Catalog(),
            ODataReader oData => oData.Catalog(),
            _ => [],
        };
        return (key, entries);
    }

    private (string Key, object Reader) Reader(string file, string pageRelativePath)
    {
        if (file.Contains("://", StringComparison.Ordinal))
            throw new ApiSpecException("Spec must be a file in the docs folder. URLs are not fetched.");

        var pageDir = Path.GetDirectoryName(pageRelativePath.Replace('/', Path.DirectorySeparatorChar)) ?? "";
        var full = Path.GetFullPath(file.StartsWith('/')
            ? Path.Combine(_root, file.TrimStart('/'))
            : Path.Combine(_root, pageDir, file));
        if (!full.StartsWith(_root, StringComparison.Ordinal) || ContentLinks.HasLink(_root, full) || !File.Exists(full))
            throw new ApiSpecException($"Spec file '{file}' not found in the docs folder.");

        var key = Path.GetRelativePath(_root, full).Replace('\\', '/');
        if (_readers.TryGetValue(key, out var cached))
            return cached is Exception error ? throw new ApiSpecException(error.Message) : (key, cached);

        var extension = Path.GetExtension(full).ToLowerInvariant();
        var text = File.ReadAllText(full);
        _contents[key] = text;
        try
        {
            object reader = extension switch
            {
                _ when OpenApiExtensions.Contains(extension) => new OpenApiReader(text),
                _ when GraphQlExtensions.Contains(extension) => new GraphQlReader(text),
                _ when ODataExtensions.Contains(extension) => new ODataReader(text),
                _ => throw new ApiSpecException($"Unsupported spec extension '{extension}'."),
            };
            _readers[key] = reader;
            return (key, reader);
        }
        catch (Exception ex) when (ex is not ApiSpecException)
        {
            _readers[key] = ex;
            throw new ApiSpecException($"{key}: {ex.Message}");
        }
    }
}

public sealed class ApiSpecException(string message) : Exception(message);
