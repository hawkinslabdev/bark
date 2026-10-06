using System.Text.Json.Nodes;

namespace Bark.Models;

public enum ApiKind { Rest, GraphQl, OData }

/// <summary>One documented field: a parameter, body attribute, response attribute or object attribute.</summary>
public sealed record ApiField
{
    public required string Name { get; init; }
    public string Type { get; init; } = "";
    public bool Required { get; init; }
    public bool Deprecated { get; init; }
    public bool ReadOnly { get; init; }
    public string? Description { get; init; }

    /// <summary>Parameter location: path, query, header or cookie. Null for body and response attributes.</summary>
    public string? In { get; init; }

    public JsonNode? Default { get; init; }
    public JsonNode? Example { get; init; }
    public IReadOnlyList<string>? Enum { get; init; }
    public IReadOnlyList<ApiField> Children { get; init; } = [];
}

/// <summary>A security scheme. Kind: apiKey, bearer, basic or oauth2.</summary>
public sealed record ApiAuth(string Kind, string Name, string In, string? Description);

public sealed record ApiResponse(string Status, string? Description, string? ContentType, IReadOnlyList<ApiField> Fields, JsonNode? Example);

public sealed record ApiBody(string ContentType, bool Required, IReadOnlyList<ApiField> Fields, JsonNode? Example);

public sealed record ApiOperation
{
    public ApiKind Kind { get; init; }
    public required string Method { get; init; }
    public required string Path { get; init; }
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public bool Deprecated { get; init; }

    /// <summary>True for an OpenAPI webhook: documented, not invocable.</summary>
    public bool Webhook { get; init; }

    public IReadOnlyList<string> Servers { get; init; } = [];
    public IReadOnlyList<ApiAuth> Auth { get; init; } = [];
    public IReadOnlyList<ApiField> Parameters { get; init; } = [];
    public ApiBody? Body { get; init; }
    public IReadOnlyList<ApiResponse> Responses { get; init; } = [];

    /// <summary>GraphQL only: the operation document sent as <c>query</c>.</summary>
    public string? GraphQlDocument { get; init; }

    /// <summary>GraphQL only: variables sent with <see cref="GraphQlDocument"/>.</summary>
    public JsonObject? GraphQlVariables { get; init; }
}

/// <summary>One generated page of an API section.</summary>
/// <param name="Group">Sidebar group: a spec tag or entity set, or a label key starting with <c>@</c> (for example <c>@objects</c>).</param>
/// <param name="Selector">The part of an <c>api:</c> value after the file name, for example <c>POST /tasks</c> or <c>schema Task</c>.</param>
public sealed record ApiCatalogEntry(string Group, string Title, string Slug, string Selector);

/// <summary>A documented object type: an OpenAPI schema, a GraphQL type or an OData entity/complex type.</summary>
public sealed record ApiObject(string Name, string? Description, IReadOnlyList<ApiField> Fields, JsonNode? Example);
