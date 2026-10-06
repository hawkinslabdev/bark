using System.Text.Json.Nodes;
using Bark.Models;

namespace Bark.Services.Api;

/// <summary>Reads operations and schemas from OpenAPI 3.0 to 3.2 and Swagger 2.0 documents (YAML or JSON).</summary>
public sealed class OpenApiReader
{
    private const int MaxDepth = 8;
    private static readonly string[] Methods = ["get", "put", "post", "delete", "options", "head", "patch", "trace", "query"];
    private static readonly string[] PreferredMediaTypes = ["application/json", "application/x-www-form-urlencoded", "multipart/form-data"];

    private readonly JsonObject _root;
    private readonly bool _swagger2;

    public OpenApiReader(string text)
    {
        _root = YamlJson.Parse(text) as JsonObject ?? throw new FormatException("OpenAPI document is not an object.");
        _swagger2 = Str(_root, "swagger") is { } version && version.StartsWith('2');
        if (!_swagger2 && Str(_root, "openapi") is null)
            throw new FormatException("Missing 'openapi' or 'swagger' version field.");
    }

    public IEnumerable<(string Method, string Path)> ListOperations()
    {
        if (_root["paths"] is not JsonObject paths)
            yield break;
        foreach (var (path, item) in paths)
        {
            if (item is not JsonObject obj)
                continue;
            foreach (var method in Methods)
                if (obj[method] is JsonObject)
                    yield return (method.ToUpperInvariant(), path);
            if (obj["additionalOperations"] is JsonObject additional)
                foreach (var (method, _) in additional)
                    yield return (method, path);
        }
    }

    // OpenAPI 3.2: fixed methods are lowercase fields, other methods are keyed under additionalOperations.
    private JsonNode? FindOperation(JsonObject pathItem, string method) =>
        Methods.Contains(method.ToLowerInvariant())
            ? pathItem[method.ToLowerInvariant()]
            : (pathItem["additionalOperations"] as JsonObject)?.FirstOrDefault(kv => string.Equals(kv.Key, method, StringComparison.OrdinalIgnoreCase)).Value;

    public ApiOperation? GetOperation(string method, string path) =>
        Resolve(_root["paths"]?[path]) is JsonObject pathItem ? BuildOperation(pathItem, method, path) : null;

    /// <summary>Webhook (OpenAPI 3.1+): an operation the API sends to the subscriber.</summary>
    public ApiOperation? GetWebhook(string name)
    {
        if (Resolve(_root["webhooks"]?[name]) is not JsonObject pathItem)
            return null;
        var method = Methods.FirstOrDefault(m => pathItem[m] is not null)
            ?? (pathItem["additionalOperations"] as JsonObject)?.FirstOrDefault().Key;
        return method is null ? null : BuildOperation(pathItem, method, name) is { } op ? op with { Webhook = true, Servers = [], Auth = [] } : null;
    }

    public IReadOnlyList<ApiCatalogEntry> Catalog()
    {
        var entries = new List<ApiCatalogEntry>();
        var tagOrder = Enumerate(_root["tags"]).OfType<JsonObject>().Select(t => Str(t, "name")).OfType<string>().ToList();

        if (_root["paths"] is JsonObject paths)
        {
            foreach (var (path, raw) in paths)
            {
                if (Resolve(raw) is not JsonObject item)
                    continue;
                var operations = Methods.Where(m => item[m] is not null).Select(m => (Method: m.ToUpperInvariant(), Node: item[m]))
                    .Concat((item["additionalOperations"] as JsonObject)?.Select(kv => (Method: kv.Key, Node: kv.Value)) ?? []);
                foreach (var (method, node) in operations)
                {
                    if (Resolve(node) is not JsonObject op)
                        continue;
                    var tag = Enumerate(op["tags"]).Select(t => t?.ToString()).FirstOrDefault(t => t is { Length: > 0 });
                    if (tag is not null && !tagOrder.Contains(tag))
                        tagOrder.Add(tag);
                    var title = Str(op, "summary") ?? Str(op, "operationId") ?? $"{method} {path}";
                    entries.Add(new ApiCatalogEntry(tag ?? "@endpoints", title, Str(op, "operationId") ?? Str(op, "summary") ?? $"{method}-{path}", $"{method} {path}"));
                }
            }
        }

        var ordered = entries.OrderBy(e => e.Group == "@endpoints" ? int.MaxValue : tagOrder.IndexOf(e.Group)).ToList();
        foreach (var name in ListWebhooks())
        {
            var hook = GetWebhook(name);
            ordered.Add(new ApiCatalogEntry("@webhooks", hook?.Summary ?? name, name, $"webhook {name}"));
        }
        var schemas = (_swagger2 ? _root["definitions"] : _root["components"]?["schemas"]) as JsonObject;
        foreach (var (name, _) in schemas ?? [])
            ordered.Add(new ApiCatalogEntry("@objects", name, name, $"schema {name}"));
        return ordered;
    }

    public IEnumerable<string> ListWebhooks() => (_root["webhooks"] as JsonObject)?.Select(kv => kv.Key) ?? [];

    private ApiOperation? BuildOperation(JsonObject pathItem, string method, string path)
    {
        if (Resolve(FindOperation(pathItem, method)) is not JsonObject op)
            return null;

        var parameters = new List<ApiField>();
        ApiBody? body = null;
        var formFields = new List<ApiField>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // Operation parameters override path-item parameters with the same name and location.
        foreach (var raw in Enumerate(op["parameters"]).Concat(Enumerate(pathItem["parameters"])))
        {
            if (Resolve(raw) is not JsonObject p || Str(p, "name") is not { } name || Str(p, "in") is not { } location)
                continue;
            if (!seen.Add(location + ":" + name))
                continue;

            if (_swagger2 && location == "body")
            {
                var schema = Resolve(p["schema"]);
                body = new ApiBody(FirstConsumes(op), Bool(p, "required"), Fields(schema, Mode.Request, 0, []), Example(schema, Mode.Request, 0, []));
                continue;
            }

            var paramSchema = _swagger2 ? p : Resolve(p["schema"] ?? (PickMedia(p["content"]) is (string, var paramMedia) ? paramMedia?["schema"] : null)) as JsonObject;
            var field = ToField(name, paramSchema, Bool(p, "required") || location == "path", Str(p, "description"), Mode.Request, 0, []) with
            {
                In = location == "formData" ? null : location,
                Deprecated = Bool(p, "deprecated"),
                Example = Clone(p["example"]) ?? Clone(FirstExample(p["examples"])) ?? Clone(paramSchema?["example"]),
            };
            if (location == "formData")
                formFields.Add(field);
            else
                parameters.Add(field);
        }

        if (formFields.Count > 0)
            body = new ApiBody(FirstConsumes(op), true, formFields, ExampleFromFields(formFields));

        if (!_swagger2 && Resolve(op["requestBody"]) is JsonObject requestBody && PickMedia(requestBody["content"]) is (string mediaType, var media))
        {
            var schema = MediaSchema(media);
            body = new ApiBody(
                mediaType,
                Bool(requestBody, "required"),
                Fields(schema, Mode.Request, 0, []),
                Clone(media?["example"]) ?? Clone(FirstExample(media?["examples"])) ?? Example(schema, Mode.Request, 0, []));
        }

        return new ApiOperation
        {
            Kind = ApiKind.Rest,
            Method = method.ToUpperInvariant(),
            Path = path,
            Summary = Str(op, "summary"),
            Description = Str(op, "description"),
            Deprecated = Bool(op, "deprecated"),
            Servers = Servers(op["servers"] ?? pathItem["servers"]),
            Auth = Auth(op["security"] ?? _root["security"]),
            Parameters = parameters,
            Body = body,
            Responses = Responses(op),
        };
    }

    public ApiObject? GetSchema(string name)
    {
        var schemas = _swagger2 ? _root["definitions"] : _root["components"]?["schemas"];
        if (Resolve((schemas as JsonObject)?[name]) is not JsonObject schema)
            return null;
        return new ApiObject(name, Str(schema, "description"), Fields(schema, Mode.Any, 0, []), Example(schema, Mode.Any, 0, []));
    }

    private IReadOnlyList<ApiResponse> Responses(JsonObject op)
    {
        var list = new List<ApiResponse>();
        if (op["responses"] is not JsonObject responses)
            return list;

        foreach (var (status, raw) in responses)
        {
            if (Resolve(raw) is not JsonObject response)
                continue;

            JsonNode? schema;
            JsonNode? example;
            string? contentType;
            if (_swagger2)
            {
                schema = Resolve(response["schema"]);
                contentType = schema is null ? null : (Enumerate(op["produces"] ?? _root["produces"]).Select(n => n?.GetValue<string>()).FirstOrDefault() ?? "application/json");
                example = (response["examples"] as JsonObject)?.FirstOrDefault().Value;
            }
            else if (PickMedia(response["content"]) is (string mediaType, var media))
            {
                schema = MediaSchema(media);
                contentType = mediaType;
                example = media?["example"] ?? FirstExample(media?["examples"]);
            }
            else
            {
                schema = null;
                contentType = null;
                example = null;
            }

            list.Add(new ApiResponse(
                status,
                Str(response, "description"),
                contentType,
                Fields(schema, Mode.Response, 0, []),
                Clone(example) ?? (schema is null ? null : Example(schema, Mode.Response, 0, []))));
        }
        return list;
    }

    private IReadOnlyList<string> Servers(JsonNode? opServers)
    {
        if (_swagger2)
        {
            var host = Str(_root, "host");
            var basePath = Str(_root, "basePath") ?? "";
            if (host is null)
                return basePath.Length > 0 ? [basePath] : [];
            var scheme = Enumerate(_root["schemes"]).Select(n => n?.GetValue<string>()).FirstOrDefault() ?? "https";
            return [$"{scheme}://{host}{basePath}"];
        }

        var servers = new List<string>();
        foreach (var raw in Enumerate(opServers ?? _root["servers"]))
        {
            if (raw is not JsonObject server || Str(server, "url") is not { } url)
                continue;
            if (server["variables"] is JsonObject variables)
                foreach (var (name, variable) in variables)
                    url = url.Replace("{" + name + "}", Str(variable as JsonObject, "default") ?? "", StringComparison.Ordinal);
            servers.Add(url.TrimEnd('/'));
        }
        return servers;
    }

    private IReadOnlyList<ApiAuth> Auth(JsonNode? security)
    {
        var schemes = (_swagger2 ? _root["securityDefinitions"] : _root["components"]?["securitySchemes"]) as JsonObject;
        var list = new List<ApiAuth>();
        if (schemes is null)
            return list;

        var names = Enumerate(security).OfType<JsonObject>().SelectMany(requirement => requirement.Select(kv => kv.Key)).Distinct();
        foreach (var name in names)
        {
            if (Resolve(schemes[name]) is not JsonObject scheme)
                continue;
            var description = Str(scheme, "description");
            var auth = Str(scheme, "type") switch
            {
                "apiKey" => new ApiAuth("apiKey", Str(scheme, "name") ?? name, Str(scheme, "in") ?? "header", description),
                "basic" => new ApiAuth("basic", "Authorization", "header", description),
                "http" when string.Equals(Str(scheme, "scheme"), "basic", StringComparison.OrdinalIgnoreCase) => new ApiAuth("basic", "Authorization", "header", description),
                "http" or "oauth2" or "openIdConnect" => new ApiAuth("bearer", "Authorization", "header", description),
                _ => null,
            };
            if (auth is not null)
                list.Add(auth);
        }
        return list;
    }

    private enum Mode { Any, Request, Response }

    private List<ApiField> Fields(JsonNode? schema, Mode mode, int depth, HashSet<string> refs)
    {
        var fields = new List<ApiField>();
        if (depth > MaxDepth || Flatten(schema, refs) is not { } flat)
            return fields;

        if (flat["items"] is { } items && IsType(flat, "array"))
            return Fields(items, mode, depth, refs);

        if (flat["properties"] is null && (flat["oneOf"] ?? flat["anyOf"]) is JsonArray variants && variants.Count > 0)
            return Fields(variants[0], mode, depth, refs);

        var required = Enumerate(flat["required"]).Select(n => n?.ToString()).ToHashSet();
        if (flat["properties"] is not JsonObject properties)
            return fields;

        foreach (var (name, propRaw) in properties)
        {
            var refName = RefName(propRaw);
            if (refName is not null && refs.Contains(refName))
            {
                fields.Add(new ApiField { Name = name, Type = refName, Required = required.Contains(name) });
                continue;
            }

            var innerRefs = refName is null ? refs : new HashSet<string>(refs) { refName };
            var prop = Flatten(propRaw, innerRefs);
            if (prop is null || (mode == Mode.Request && Bool(prop, "readOnly")) || (mode == Mode.Response && Bool(prop, "writeOnly")))
                continue;
            fields.Add(ToField(name, prop, required.Contains(name), Str(prop, "description"), mode, depth + 1, innerRefs));
        }
        return fields;
    }

    private ApiField ToField(string name, JsonObject? schema, bool required, string? description, Mode mode, int depth, HashSet<string> refs)
    {
        var enumValues = Enumerate(schema?["enum"] ?? (schema?["items"] as JsonObject)?["enum"])
            .Where(v => v is not null).Select(v => v!.ToString()).ToList();
        return new ApiField
        {
            Name = name,
            Type = TypeLabel(schema, refs),
            Required = required,
            Description = description,
            Deprecated = Bool(schema, "deprecated"),
            ReadOnly = Bool(schema, "readOnly"),
            Default = Clone(schema?["default"]),
            Example = Clone(schema?["example"]),
            Enum = enumValues.Count > 0 ? enumValues : null,
            Children = Fields(schema, mode, depth, refs),
        };
    }

    private string TypeLabel(JsonNode? raw, HashSet<string> refs)
    {
        var schema = Flatten(raw, refs);
        if (schema is null)
            return "any";

        if (IsType(schema, "array"))
            return TypeLabel(schema["items"], refs) + "[]";

        if ((schema["oneOf"] ?? schema["anyOf"]) is JsonArray variants)
            return string.Join(" | ", Enumerate(variants).Select(v => RefName(v) ?? TypeLabel(v, refs)).Distinct());

        var type = PrimaryType(schema) ?? (schema["properties"] is not null ? "object" : "any");
        var format = Str(schema, "format");
        var label = format is null ? type : $"{type}<{format}>";
        if (schema["enum"] is not null)
            label = $"enum<{type}>";
        if (schema["additionalProperties"] is JsonObject map && schema["properties"] is null)
            label = $"map<string, {TypeLabel(map, refs)}>";
        return Bool(schema, "nullable") || TypeList(schema).Contains("null") ? label + " | null" : label;
    }

    private JsonNode? Example(JsonNode? raw, Mode mode, int depth, HashSet<string> refs)
    {
        if (depth > MaxDepth)
            return null;
        var refName = RefName(raw);
        if (refName is not null && refs.Contains(refName))
            return null;
        var innerRefs = refName is null ? refs : new HashSet<string>(refs) { refName };
        var schema = Flatten(raw, innerRefs);
        if (schema is null)
            return null;

        if ((schema["example"] ?? FirstExample(schema["examples"]) ?? schema["default"] ?? schema["const"]) is { } given)
            return Clone(given);
        if (schema["enum"] is JsonArray values && values.Count > 0)
            return Clone(values[0]);
        if ((schema["oneOf"] ?? schema["anyOf"]) is JsonArray variants && variants.Count > 0)
            return Example(variants[0], mode, depth + 1, innerRefs);

        if (IsType(schema, "array"))
            return Example(schema["items"], mode, depth + 1, innerRefs) is { } item ? new JsonArray(item) : new JsonArray();

        if (schema["properties"] is JsonObject properties)
        {
            var obj = new JsonObject();
            foreach (var (name, prop) in properties)
            {
                var flatProp = Flatten(prop, innerRefs);
                if ((mode == Mode.Request && Bool(flatProp, "readOnly")) || (mode == Mode.Response && Bool(flatProp, "writeOnly")))
                    continue;
                obj[name] = Example(prop, mode, depth + 1, innerRefs);
            }
            return obj;
        }
        if (schema["additionalProperties"] is JsonObject map)
            return new JsonObject { ["key"] = Example(map, mode, depth + 1, innerRefs) };

        return ScalarExample(PrimaryType(schema), Str(schema, "format"));
    }

    internal static JsonNode? ScalarExample(string? type, string? format) => (type, format) switch
    {
        ("string", "date-time") => "2024-01-01T00:00:00Z",
        ("string", "date") => "2024-01-01",
        ("string", "time") => "00:00:00",
        ("string", "email") => "user@example.com",
        ("string", "uuid") => "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ("string", "uri" or "url") => "https://example.com",
        ("string", "binary") => null,
        ("string", _) => "<string>",
        ("integer", _) => 123,
        ("number", _) => 123,
        ("boolean", _) => true,
        ("object", _) => new JsonObject(),
        _ => null,
    };

    internal static JsonNode? ExampleFromFields(IReadOnlyList<ApiField> fields)
    {
        var obj = new JsonObject();
        foreach (var field in fields)
        {
            var isArray = field.Type.EndsWith("[]", StringComparison.Ordinal);
            var baseType = field.Type.Split('<', '[', ' ')[0];
            JsonNode? value = Clone(field.Example) ?? Clone(field.Default)
                ?? (field.Enum is { Count: > 0 } e ? JsonValue.Create(e[0])
                : field.Children.Count > 0 ? ExampleFromFields(field.Children)
                : ScalarExample(baseType, null) ?? JsonValue.Create("<" + field.Type + ">"));
            if (isArray && value is not JsonArray)
                value = new JsonArray(value);
            obj[field.Name] = value;
        }
        return obj;
    }

    // Resolves $ref and merges allOf parts into one schema object.
    private JsonObject? Flatten(JsonNode? raw, HashSet<string> refs)
    {
        if (Resolve(raw) is not JsonObject schema)
            return null;
        if (schema["allOf"] is not JsonArray parts)
            return schema;

        var merged = new JsonObject();
        var properties = new JsonObject();
        var required = new JsonArray();
        foreach (var part in parts.Prepend(schema))
        {
            var refName = RefName(part);
            if (refName is not null && refs.Contains(refName))
                continue;
            if ((part == schema ? schema : Flatten(part, refName is null ? refs : new HashSet<string>(refs) { refName })) is not { } flat)
                continue;
            foreach (var (key, value) in flat)
            {
                if (key is "allOf")
                    continue;
                if (key == "properties" && value is JsonObject props)
                {
                    foreach (var (name, prop) in props)
                        properties[name] = Clone(prop);
                }
                else if (key == "required" && value is JsonArray req)
                {
                    foreach (var r in req)
                        required.Add(Clone(r));
                }
                else
                {
                    merged[key] = Clone(value);
                }
            }
        }
        if (properties.Count > 0)
        {
            merged["properties"] = properties;
            merged["type"] ??= "object";
        }
        if (required.Count > 0)
            merged["required"] = required;
        return merged;
    }

    private JsonNode? Resolve(JsonNode? node)
    {
        for (var hops = 0; hops < 32 && node is JsonObject obj && Str(obj, "$ref") is { } reference; hops++)
        {
            if (!reference.StartsWith("#/", StringComparison.Ordinal))
                return node;
            JsonNode? target = _root;
            foreach (var segment in reference[2..].Split('/'))
                target = (target as JsonObject)?[Uri.UnescapeDataString(segment).Replace("~1", "/").Replace("~0", "~")];
            node = target;
        }
        return node;
    }

    // OpenAPI 3.2 itemSchema describes each item of a sequential media type (JSON Lines, SSE).
    private JsonNode? MediaSchema(JsonObject? media) =>
        Resolve(media?["schema"]) ?? (media?["itemSchema"] is { } item ? new JsonObject { ["type"] = "array", ["items"] = item.DeepClone() } : null);

    private static string? RefName(JsonNode? node) =>
        Str(node as JsonObject, "$ref") is { } reference ? reference[(reference.LastIndexOf('/') + 1)..] : null;

    private static (string, JsonObject?)? PickMedia(JsonNode? content)
    {
        if (content is not JsonObject media || media.Count == 0)
            return null;
        foreach (var preferred in PreferredMediaTypes)
            if (media.FirstOrDefault(kv => kv.Key.StartsWith(preferred, StringComparison.OrdinalIgnoreCase)) is { Key: not null } hit)
                return (hit.Key, hit.Value as JsonObject);
        var first = media.First();
        return (first.Key, first.Value as JsonObject);
    }

    private string FirstConsumes(JsonObject op) =>
        Enumerate(op["consumes"] ?? _root["consumes"]).Select(n => n?.ToString()).FirstOrDefault(s => s is not null) ?? "application/json";

    private static JsonNode? FirstExample(JsonNode? examples) => examples switch
    {
        JsonArray arr when arr.Count > 0 => arr[0],
        JsonObject obj when obj.Count > 0 => obj.First().Value is JsonObject ex && ex.ContainsKey("value") ? ex["value"] : null,
        _ => null,
    };

    private static IEnumerable<string> TypeList(JsonObject schema) => schema["type"] switch
    {
        JsonArray arr => arr.Select(t => t?.ToString() ?? "null"),
        JsonValue v => [v.ToString()],
        _ => [],
    };

    private static string? PrimaryType(JsonObject schema) => TypeList(schema).FirstOrDefault(t => t != "null");

    private static bool IsType(JsonObject schema, string type) => TypeList(schema).Contains(type) || (type == "array" && schema["items"] is not null && schema["type"] is null);

    private static IEnumerable<JsonNode?> Enumerate(JsonNode? node) => node as JsonArray ?? (IEnumerable<JsonNode?>)[];

    internal static string? Str(JsonObject? obj, string key) =>
        obj?[key] is JsonValue value && value.TryGetValue<string>(out var s) ? s : null;

    private static bool Bool(JsonObject? obj, string key) =>
        obj?[key] is JsonValue value && value.TryGetValue<bool>(out var b) && b;

    internal static JsonNode? Clone(JsonNode? node) => node?.DeepClone();
}
