using Bark.Configuration;
using Bark.Services.Api;
using Bark.Services.Rendering;

namespace Bark.Tests;

public sealed class ApiPageTests : IDisposable
{
    private const string OpenApi32 = """
        openapi: 3.2.0
        info: { title: T, version: '1' }
        servers:
          - url: https://{region}.example.com/v1
            variables: { region: { default: eu } }
        security: [ { Key: [] } ]
        paths:
          /items:
            query:
              summary: Search items
              parameters:
                - { name: q, in: querystring, content: { application/x-www-form-urlencoded: { schema: { type: object } } } }
              requestBody:
                content:
                  application/json:
                    schema: { $ref: '#/components/schemas/Item' }
              responses:
                '200':
                  description: Stream
                  content:
                    application/jsonl:
                      itemSchema: { $ref: '#/components/schemas/Item' }
            additionalOperations:
              PURGE:
                summary: Purge cache
                responses: { '204': { description: Purged } }
        webhooks:
          itemCreated:
            post:
              requestBody: { content: { application/json: { schema: { $ref: '#/components/schemas/Item' } } } }
              responses: { '200': { description: OK } }
        components:
          securitySchemes:
            Key: { type: apiKey, in: header, name: X-Key }
          schemas:
            Base:
              type: object
              required: [id]
              properties:
                id: { type: integer, readOnly: true }
            Item:
              allOf:
                - $ref: '#/components/schemas/Base'
                - type: object
                  properties:
                    name: { type: string, example: Widget }
                    parent: { $ref: '#/components/schemas/Item' }
        """;

    private const string Swagger2 = """
        {
          "swagger": "2.0",
          "host": "api.example.com",
          "basePath": "/v2",
          "schemes": ["https"],
          "paths": {
            "/pets": {
              "post": {
                "parameters": [{ "name": "body", "in": "body", "required": true, "schema": { "type": "object", "properties": { "name": { "type": "string" } } } }],
                "responses": { "200": { "description": "OK", "schema": { "type": "array", "items": { "type": "string" } } } }
              }
            }
          }
        }
        """;

    private const string Sdl = """"
        "Root query."
        schema { query: Root }
        directive @auth(role: String) on FIELD_DEFINITION | OBJECT
        interface Node { id: ID! }
        enum Role { ADMIN USER @deprecated(reason: "old") }
        """A user."""
        type User implements Node & Entity @auth(role: "x") {
          id: ID!
          role: Role
          friends(first: Int = 10): [User!]!
        }
        input UserFilter { role: Role = ADMIN }
        type Root {
          "Fetch one user."
          user(id: ID!, filter: UserFilter): User
        }
        """";

    private const string Csdl = """
        <?xml version="1.0"?>
        <edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
          <edmx:DataServices>
            <Schema Namespace="NS" Alias="A" xmlns="http://docs.oasis-open.org/odata/ns/edm">
              <EntityType Name="Person">
                <Key><PropertyRef Name="UserName"/></Key>
                <Property Name="UserName" Type="Edm.String" Nullable="false"/>
                <Property Name="Version" Type="Edm.Int64"><Annotation Term="Org.OData.Core.V1.Computed" Bool="true"/></Property>
                <NavigationProperty Name="Friends" Type="Collection(A.Person)"/>
              </EntityType>
              <Function Name="Nearest"><Parameter Name="lat" Type="Edm.Double" Nullable="false"/><ReturnType Type="Edm.String"/></Function>
              <EntityContainer Name="C">
                <EntitySet Name="People" EntityType="A.Person"/>
                <FunctionImport Name="Nearest" Function="NS.Nearest"/>
              </EntityContainer>
            </Schema>
          </edmx:DataServices>
        </edmx:Edmx>
        """;

    private readonly string _docs = Directory.CreateTempSubdirectory("bark-api-").FullName;

    public ApiPageTests()
    {
        Directory.CreateDirectory(Path.Combine(_docs, "api"));
        File.WriteAllText(Path.Combine(_docs, "api", "openapi.yaml"), OpenApi32);
        File.WriteAllText(Path.Combine(_docs, "api", "swagger.json"), Swagger2);
        File.WriteAllText(Path.Combine(_docs, "api", "schema.graphql"), Sdl);
        File.WriteAllText(Path.Combine(_docs, "api", "metadata.xml"), Csdl);
    }

    public void Dispose() => Directory.Delete(_docs, true);

    private ApiPageModel Load(string api) => new ApiSpecLoader(_docs).Load(api, "api/page.md");

    [Fact]
    public void OpenApi32_QueryMethod_ResolvesServersAuthAllOfAndItemSchema()
    {
        var op = Load("openapi.yaml QUERY /items").Operation!;

        Assert.Equal("QUERY", op.Method);
        Assert.Equal(["https://eu.example.com/v1"], op.Servers);
        Assert.Equal(new ApiAuthTuple("apiKey", "X-Key", "header"), new ApiAuthTuple(op.Auth[0].Kind, op.Auth[0].Name, op.Auth[0].In));
        Assert.Equal("querystring", Assert.Single(op.Parameters).In);

        var bodyNames = op.Body!.Fields.Select(f => f.Name).ToArray();
        Assert.Equal(["name", "parent"], bodyNames);
        Assert.Equal("Widget", op.Body.Example!["name"]!.GetValue<string>());
        Assert.Null(op.Body.Example["id"]);
        Assert.Null(op.Body.Example["parent"]!["parent"]);

        var stream = op.Responses.Single();
        Assert.Equal("application/jsonl", stream.ContentType);
        Assert.Contains(stream.Fields, f => f.Name == "id" && f.ReadOnly);
    }

    private sealed record ApiAuthTuple(string Kind, string Name, string In);

    [Fact]
    public void OpenApi32_AdditionalOperationsAndWebhooks_AreAddressable()
    {
        Assert.Equal("Purge cache", Load("openapi.yaml PURGE /items").Operation!.Summary);

        var webhook = Load("openapi.yaml webhook itemCreated").Operation!;
        Assert.True(webhook.Webhook);
        Assert.Empty(webhook.Auth);
        Assert.Empty(webhook.Servers);
    }

    [Fact]
    public void Swagger2_BodyParameterAndHost_MapToBodyAndServer()
    {
        var op = Load("swagger.json POST /pets").Operation!;
        Assert.Equal(["https://api.example.com/v2"], op.Servers);
        Assert.Equal("name", Assert.Single(op.Body!.Fields).Name);
        Assert.Equal("<string>", op.Responses[0].Example![0]!.GetValue<string>());
    }

    [Fact]
    public void GraphQl_BuildsOperationDocumentAndVariables()
    {
        var op = Load("schema.graphql query user").Operation!;

        Assert.Equal("Fetch one user.", op.Description);
        Assert.Equal(["id", "filter"], op.Parameters.Select(p => p.Name).ToArray());
        Assert.True(op.Parameters[0].Required);
        Assert.Contains("query User($id: ID!, $filter: UserFilter)", op.GraphQlDocument);
        Assert.Contains("user(id: $id, filter: $filter)", op.GraphQlDocument);
        Assert.DoesNotContain("friends(", op.GraphQlDocument);
        Assert.Equal("<id>", op.GraphQlVariables!["id"]!.GetValue<string>());
        Assert.Equal("ADMIN", op.GraphQlVariables["filter"]!["role"]!.GetValue<string>());

        var user = Load("schema.graphql schema User").Object!;
        Assert.Equal("A user.", user.Description);
        Assert.Equal(["ADMIN", "USER"], user.Fields.Single(f => f.Name == "role").Enum);
    }

    [Fact]
    public void OData_EntitySetAndImports_MapToRestOperations()
    {
        var list = Load("metadata.xml GET People").Operation!;
        Assert.Equal("/People", list.Path);
        Assert.Contains(list.Parameters, p => p.Name == "$filter");

        var byKey = Load("metadata.xml PATCH People(key)").Operation!;
        Assert.Equal("/People({UserName})", byKey.Path);
        Assert.DoesNotContain(byKey.Body!.Fields, f => f.Name == "Version");

        var function = Load("metadata.xml GET Nearest").Operation!;
        Assert.Equal("/Nearest(lat={lat})", function.Path);
    }

    [Theory]
    [InlineData("../../etc/passwd GET /x")]
    [InlineData("https://example.com/openapi.yaml GET /x")]
    [InlineData("missing.yaml GET /x")]
    [InlineData("openapi.yaml GET /does-not-exist")]
    public void Loader_RejectsFilesOutsideDocsUrlsAndUnknownOperations(string api)
    {
        Assert.Throws<ApiSpecException>(() => Load(api));
    }

    [Fact]
    public void Renderer_EmbedsPlaygroundDataWithoutClosingTheScriptElement()
    {
        File.WriteAllText(Path.Combine(_docs, "api", "xss.yaml"), """
            openapi: 3.1.0
            info: { title: T, version: '1' }
            servers: [ { url: https://api.example.com } ]
            paths:
              /x:
                get:
                  summary: "</script><script>alert(1)</script>"
                  responses: { '200': { description: OK } }
            """);
        var model = Load("xss.yaml GET /x");
        var html = ApiPageRenderer.Render(model, "", new ApiPageRenderer.Options(Localization.Default, md => md, null, null, null));

        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "</script>"));
        Assert.Equal(["https://api.example.com"], ApiPageRenderer.Origins(model, null));
        Assert.Equal(["https://api.example.com:8443"], ApiPageRenderer.Origins(model, "https://a;script-src@api.example.com:8443/v1"));
    }

    [Fact]
    public void WithConnectSources_WidensConnectSrcOnly()
    {
        var csp = SecurityHeaders.WithConnectSources(SecurityHeaders.DefaultCsp, ["https://api.example.com"]);
        Assert.Contains("connect-src 'self' https://api.example.com", csp);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline' https://api.example.com", csp);
    }
}
