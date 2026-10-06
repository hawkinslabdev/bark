using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using Bark.Models;

namespace Bark.Services.Api;

/// <summary>Reads entity sets, operations and types from an OData CSDL ($metadata) document, versions 2 to 4.</summary>
public sealed class ODataReader
{
    private const int MaxDepth = 3;
    private const string KeySuffix = "(key)";

    private static readonly ApiField[] CollectionOptions =
    [
        new() { Name = "$filter", Type = "string", In = "query", Description = "Filter expression, for example `Name eq 'value'`." },
        new() { Name = "$select", Type = "string", In = "query", Description = "Comma-separated properties to return." },
        new() { Name = "$expand", Type = "string", In = "query", Description = "Comma-separated navigation properties to include." },
        new() { Name = "$orderby", Type = "string", In = "query", Description = "Sort expression, for example `Name desc`." },
        new() { Name = "$top", Type = "integer", In = "query", Description = "Maximum number of items." },
        new() { Name = "$skip", Type = "integer", In = "query", Description = "Number of items to skip." },
        new() { Name = "$count", Type = "boolean", In = "query", Description = "Includes `@odata.count` in the response." },
        new() { Name = "$search", Type = "string", In = "query", Description = "Free-text search expression." },
    ];

    private static readonly ApiField[] EntityOptions =
    [
        new() { Name = "$select", Type = "string", In = "query", Description = "Comma-separated properties to return." },
        new() { Name = "$expand", Type = "string", In = "query", Description = "Comma-separated navigation properties to include." },
    ];

    private readonly Dictionary<string, XElement> _types = new(StringComparer.Ordinal);
    private readonly Dictionary<string, XElement> _operations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Type, bool Singleton)> _sets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, XElement> _imports = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.Ordinal);

    public ODataReader(string xml)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        var doc = XDocument.Load(reader);

        foreach (var schema in doc.Descendants().Where(e => e.Name.LocalName == "Schema"))
        {
            var ns = (string?)schema.Attribute("Namespace") ?? "";
            if ((string?)schema.Attribute("Alias") is { } alias)
                _aliases[alias] = ns;

            foreach (var element in schema.Elements())
            {
                var name = (string?)element.Attribute("Name");
                if (name is null)
                    continue;
                switch (element.Name.LocalName)
                {
                    case "EntityType" or "ComplexType" or "EnumType":
                        _types[name] = element;
                        _types[ns + "." + name] = element;
                        break;
                    case "Function" or "Action":
                        _operations[name] = element;
                        _operations[ns + "." + name] = element;
                        break;
                    case "EntityContainer":
                        ReadContainer(element);
                        break;
                }
            }
        }
    }

    private void ReadContainer(XElement container)
    {
        foreach (var element in container.Elements())
        {
            var name = (string?)element.Attribute("Name");
            if (name is null)
                continue;
            switch (element.Name.LocalName)
            {
                case "EntitySet":
                    _sets[name] = ((string?)element.Attribute("EntityType") ?? "", false);
                    break;
                case "Singleton":
                    _sets[name] = ((string?)element.Attribute("Type") ?? "", true);
                    break;
                case "FunctionImport" or "ActionImport":
                    _imports[name] = element;
                    break;
            }
        }
    }

    public IEnumerable<(string Method, string Target)> ListOperations()
    {
        foreach (var (name, (_, singleton)) in _sets)
        {
            yield return ("GET", name);
            if (singleton)
            {
                yield return ("PATCH", name);
                continue;
            }
            yield return ("POST", name);
            yield return ("GET", name + KeySuffix);
            yield return ("PATCH", name + KeySuffix);
            yield return ("DELETE", name + KeySuffix);
        }
        foreach (var (name, import) in _imports)
            yield return (IsAction(import) ? "POST" : "GET", name);
    }

    public IReadOnlyList<ApiCatalogEntry> Catalog()
    {
        var entries = new List<ApiCatalogEntry>();
        foreach (var (method, target) in ListOperations())
        {
            if (GetOperation(method, target) is not { } op)
                continue;
            var group = _imports.ContainsKey(target) ? "@operations" : target.Replace(KeySuffix, "", StringComparison.Ordinal);
            entries.Add(new ApiCatalogEntry(group, op.Summary ?? $"{method} {target}", $"{method}-{target.Replace(KeySuffix, "-by-key", StringComparison.Ordinal)}", $"{method} {target}"));
        }
        foreach (var name in _types.Keys.Where(k => !k.Contains('.')))
            entries.Add(new ApiCatalogEntry("@types", name, name, $"schema {name}"));
        return entries;
    }

    public ApiOperation? GetOperation(string method, string target)
    {
        method = method.ToUpperInvariant();
        var byKey = target.EndsWith(KeySuffix, StringComparison.Ordinal);
        var name = byKey ? target[..^KeySuffix.Length] : target;

        if (_imports.TryGetValue(name, out var import))
            return ImportOperation(name, import);
        if (!_sets.TryGetValue(name, out var set) || Type(set.Type) is not { } entity)
            return null;

        var entityName = (string)entity.Attribute("Name")!;
        var fields = Properties(entity, 0, []);
        var example = Example(entity, 0, []);
        var keyFields = byKey && !set.Singleton ? KeyFields(entity) : [];
        var path = "/" + name + (keyFields.Count switch
        {
            0 => "",
            1 => "({" + keyFields[0].Name + "})",
            _ => "(" + string.Join(",", keyFields.Select(k => $"{k.Name}={{{k.Name}}}")) + ")",
        });
        var writable = fields.Where(f => !f.ReadOnly).ToList();
        var writableExample = Without(example, fields.Where(f => f.ReadOnly).Select(f => f.Name));

        var (summary, parameters, body, responses) = (method, byKey || set.Singleton) switch
        {
            ("GET", false) => ($"List {name}", CollectionOptions.ToList(), (ApiBody?)null, List(Ok("200", $"Collection of {entityName}.",
                [new ApiField { Name = "value", Type = entityName + "[]", Children = fields }],
                new JsonObject { ["@odata.context"] = "$metadata#" + name, ["value"] = new JsonArray(example?.DeepClone()) }))),
            ("GET", true) => ($"Get {entityName}", keyFields.Concat(EntityOptions).ToList(), null, List(Ok("200", entityName, fields, example))),
            ("POST", false) => ($"Create {entityName}", [], new ApiBody("application/json", true, writable, writableExample), List(Ok("201", "Created.", fields, example))),
            ("PATCH" or "PUT", true) => ($"Update {entityName}", keyFields, new ApiBody("application/json", true, writable.Select(f => f with { Required = false }).ToList(), writableExample), List(Ok("204", "Updated.", [], null))),
            ("DELETE", true) => ($"Delete {entityName}", keyFields, null, List(Ok("204", "Deleted.", [], null))),
            _ => default,
        };
        if (summary is null)
            return null;

        return new ApiOperation
        {
            Kind = ApiKind.OData,
            Method = method,
            Path = path,
            Summary = summary,
            Description = Description(entity),
            Parameters = parameters,
            Body = body,
            Responses = responses,
        };
    }

    public ApiObject? GetSchema(string name)
    {
        if (Type(name) is not { } type)
            return null;
        if (type.Name.LocalName == "EnumType")
        {
            var members = Children(type, "Member").Select(m => new ApiField { Name = (string)m.Attribute("Name")!, Type = "enum value", Description = Description(m) }).ToList();
            return new ApiObject(name, Description(type), members, null);
        }
        return new ApiObject(name, Description(type), Properties(type, 0, []), Example(type, 0, []));
    }

    private ApiOperation ImportOperation(string name, XElement import)
    {
        var operation = (string?)import.Attribute("Function") ?? (string?)import.Attribute("Action");
        var definition = operation is not null && _operations.TryGetValue(Unalias(operation), out var op) ? op : import;
        var isAction = IsAction(import);
        var parameters = Children(definition, "Parameter")
            .Where(p => !((string?)definition.Attribute("IsBound") == "true" && p == definition.Elements().First(e => e.Name.LocalName == "Parameter")))
            .Select(p => Field(p, 0, []) with { Required = (string?)p.Attribute("Nullable") != "true" })
            .ToList();

        var returnType = (string?)definition.Attribute("ReturnType")
            ?? (string?)Children(definition, "ReturnType").FirstOrDefault()?.Attribute("Type");

        var responses = new List<ApiResponse>();
        if (returnType is null)
        {
            responses.Add(Ok("204", "No content.", [], null));
        }
        else
        {
            var resultField = new ApiField { Name = "value", Type = TypeLabel(returnType), Children = ChildrenOf(returnType, 0, []) };
            var resultExample = ExampleOf(returnType, 0, []);
            var isComplex = Type(ElementType(returnType)) is not null && !returnType.StartsWith("Collection(", StringComparison.Ordinal);
            responses.Add(isComplex
                ? Ok("200", null, resultField.Children, resultExample)
                : Ok("200", null, [resultField], new JsonObject { ["value"] = resultExample }));
        }

        var path = "/" + name;
        if (!isAction && parameters.Count > 0)
            path += "(" + string.Join(",", parameters.Select(p => $"{p.Name}={{{p.Name}}}")) + ")";

        return new ApiOperation
        {
            Kind = ApiKind.OData,
            Method = isAction ? "POST" : "GET",
            Path = path,
            Summary = name,
            Description = Description(definition) ?? Description(import),
            Parameters = isAction ? [] : parameters.Select(p => p with { In = "path", Required = true }).ToList(),
            Body = isAction && parameters.Count > 0 ? new ApiBody("application/json", true, parameters, OpenApiReader.ExampleFromFields(parameters)) : null,
            Responses = responses,
        };
    }

    private static bool IsAction(XElement import) =>
        import.Name.LocalName == "ActionImport"
        || string.Equals((string?)import.Attributes().FirstOrDefault(a => a.Name.LocalName == "HttpMethod"), "POST", StringComparison.OrdinalIgnoreCase);

    private List<ApiField> KeyFields(XElement entity)
    {
        var keys = KeyNames(entity);
        return Properties(entity, MaxDepth, [])
            .Where(f => keys.Contains(f.Name))
            .Select(f => f with { In = "path", Required = true, Children = [] })
            .ToList();
    }

    private HashSet<string> KeyNames(XElement entity)
    {
        for (var current = entity; current is not null; current = BaseType(current))
        {
            var keys = Children(current, "Key").SelectMany(k => Children(k, "PropertyRef")).Select(r => (string?)r.Attribute("Name")).OfType<string>().ToHashSet();
            if (keys.Count > 0)
                return keys;
        }
        return [];
    }

    private List<ApiField> Properties(XElement type, int depth, HashSet<string> seen)
    {
        var chain = new List<XElement>();
        for (var current = type; current is not null && chain.Count < 16; current = BaseType(current))
            chain.Insert(0, current);

        var keys = type.Name.LocalName == "EntityType" ? KeyNames(type) : [];
        var fields = new List<ApiField>();
        foreach (var element in chain.SelectMany(t => t.Elements()))
        {
            if (element.Name.LocalName is not ("Property" or "NavigationProperty"))
                continue;
            var field = Field(element, depth, seen);
            var isNavigation = element.Name.LocalName == "NavigationProperty";
            fields.Add(field with
            {
                Required = !isNavigation && (keys.Contains(field.Name) || (string?)element.Attribute("Nullable") == "false"),
                ReadOnly = HasAnnotation(element, "Computed") || HasAnnotation(element, "Immutable"),
                Children = isNavigation ? [] : field.Children,
            });
        }
        return fields;
    }

    private ApiField Field(XElement element, int depth, HashSet<string> seen)
    {
        var type = (string?)element.Attribute("Type") ?? "Edm.String";
        var enumType = Type(ElementType(type)) is { } t && t.Name.LocalName == "EnumType" ? t : null;
        return new ApiField
        {
            Name = (string?)element.Attribute("Name") ?? "",
            Type = TypeLabel(type),
            Description = Description(element),
            Default = (string?)element.Attribute("DefaultValue") is { } d ? JsonValue.Create(d) : null,
            Enum = enumType is null ? null : Children(enumType, "Member").Select(m => (string)m.Attribute("Name")!).ToList(),
            Children = ChildrenOf(type, depth, seen),
        };
    }

    private IReadOnlyList<ApiField> ChildrenOf(string type, int depth, HashSet<string> seen)
    {
        var name = ElementType(type);
        if (depth >= MaxDepth || seen.Contains(name) || Type(name) is not { } element || element.Name.LocalName == "EnumType")
            return [];
        return Properties(element, depth + 1, new HashSet<string>(seen) { name });
    }

    private JsonNode? Example(XElement type, int depth, HashSet<string> seen)
    {
        var obj = new JsonObject();
        foreach (var field in Properties(type, MaxDepth, seen))
        {
            var element = AllProperties(type).FirstOrDefault(p => (string?)p.Attribute("Name") == field.Name);
            if (element is null || element.Name.LocalName == "NavigationProperty")
                continue;
            obj[field.Name] = ExampleOf((string?)element.Attribute("Type") ?? "Edm.String", depth + 1, seen);
        }
        return obj;
    }

    private IEnumerable<XElement> AllProperties(XElement type)
    {
        for (var current = type; current is not null; current = BaseType(current))
            foreach (var element in current.Elements())
                yield return element;
    }

    private JsonNode? ExampleOf(string type, int depth, HashSet<string> seen)
    {
        if (type.StartsWith("Collection(", StringComparison.Ordinal))
            return new JsonArray(ExampleOf(ElementType(type), depth, seen));

        var name = ElementType(type);
        if (Type(name) is { } element)
        {
            if (element.Name.LocalName == "EnumType")
                return (string?)Children(element, "Member").FirstOrDefault()?.Attribute("Name");
            if (depth > MaxDepth || seen.Contains(name))
                return null;
            return Example(element, depth, new HashSet<string>(seen) { name });
        }

        var (scalar, format) = Scalar(name);
        return OpenApiReader.ScalarExample(scalar, format);
    }

    private string TypeLabel(string type)
    {
        if (type.StartsWith("Collection(", StringComparison.Ordinal))
            return TypeLabel(ElementType(type)) + "[]";
        if (Type(type) is { } element)
            return element.Name.LocalName == "EnumType" ? "enum<string>" : (string)element.Attribute("Name")!;
        var (scalar, format) = Scalar(type);
        return format is null ? scalar : $"{scalar}<{format}>";
    }

    private static (string Type, string? Format) Scalar(string edm) => edm switch
    {
        "Edm.String" => ("string", null),
        "Edm.Boolean" => ("boolean", null),
        "Edm.Byte" or "Edm.SByte" or "Edm.Int16" or "Edm.Int32" => ("integer", "int32"),
        "Edm.Int64" => ("integer", "int64"),
        "Edm.Decimal" => ("number", "decimal"),
        "Edm.Double" or "Edm.Single" => ("number", "double"),
        "Edm.DateTimeOffset" or "Edm.DateTime" => ("string", "date-time"),
        "Edm.Date" => ("string", "date"),
        "Edm.TimeOfDay" or "Edm.Time" => ("string", "time"),
        "Edm.Duration" => ("string", "duration"),
        "Edm.Guid" => ("string", "uuid"),
        "Edm.Binary" or "Edm.Stream" => ("string", "base64"),
        _ when edm.StartsWith("Edm.Geo", StringComparison.Ordinal) => ("object", null),
        _ => ("string", null),
    };

    private static string ElementType(string type) =>
        type.StartsWith("Collection(", StringComparison.Ordinal) ? type[11..^1] : type;

    private string Unalias(string name)
    {
        var dot = name.LastIndexOf('.');
        return dot > 0 && _aliases.TryGetValue(name[..dot], out var ns) ? ns + name[dot..] : name;
    }

    private XElement? Type(string name) => _types.TryGetValue(Unalias(name), out var type) ? type : null;

    private XElement? BaseType(XElement type) => (string?)type.Attribute("BaseType") is { } baseType ? Type(baseType) : null;

    private static IEnumerable<XElement> Children(XElement parent, string localName) =>
        parent.Elements().Where(e => e.Name.LocalName == localName);

    private static string? Description(XElement element) =>
        Children(element, "Annotation")
            .FirstOrDefault(a => ((string?)a.Attribute("Term"))?.EndsWith(".Description", StringComparison.Ordinal) == true)
            ?.Attribute("String")?.Value
        ?? Children(element, "Documentation").FirstOrDefault()?.Elements().FirstOrDefault(e => e.Name.LocalName == "Summary")?.Value;

    private static bool HasAnnotation(XElement element, string term) =>
        Children(element, "Annotation").Any(a => ((string?)a.Attribute("Term"))?.EndsWith("." + term, StringComparison.Ordinal) == true);

    private static JsonNode? Without(JsonNode? example, IEnumerable<string> names)
    {
        if (example?.DeepClone() is not JsonObject obj)
            return example;
        foreach (var name in names)
            obj.Remove(name);
        return obj;
    }

    private static ApiResponse Ok(string status, string? description, IReadOnlyList<ApiField> fields, JsonNode? example) =>
        new(status, description, fields.Count > 0 || example is not null ? "application/json" : null, fields, example);

    private static List<ApiResponse> List(ApiResponse response) => [response];
}
