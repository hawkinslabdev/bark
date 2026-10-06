using System.Text;
using System.Text.Json.Nodes;
using Bark.Models;

namespace Bark.Services.Api;

/// <summary>Reads operations and types from a GraphQL schema definition (SDL).</summary>
public sealed class GraphQlReader
{
    private const int MaxDepth = 3;
    private static readonly HashSet<string> BuiltInScalars = ["Int", "Float", "String", "Boolean", "ID"];

    private sealed record Arg(string Name, string Type, string? Description, string? Default);
    private sealed record Field(string Name, string Type, string? Description, List<Arg> Args, bool Deprecated);
    private sealed class TypeDef(string kind, string name)
    {
        public string Kind { get; } = kind;
        public string Name { get; } = name;
        public string? Description { get; set; }
        public List<Field> Fields { get; } = [];
        public List<(string Name, string? Description)> Values { get; } = [];
        public List<string> Members { get; } = [];
    }

    private readonly Dictionary<string, TypeDef> _types = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _roots = new(StringComparer.Ordinal)
    {
        ["query"] = "Query",
        ["mutation"] = "Mutation",
        ["subscription"] = "Subscription",
    };

    public GraphQlReader(string sdl)
    {
        new Parser(Tokenize(sdl), this).ParseDocument();
    }

    public IEnumerable<(string OperationType, string Field)> ListOperations() =>
        _roots.SelectMany(root => _types.TryGetValue(root.Value, out var type)
            ? type.Fields.Select(f => (root.Key, f.Name))
            : []);

    public IReadOnlyList<ApiCatalogEntry> Catalog()
    {
        var entries = ListOperations()
            .Select(op => new ApiCatalogEntry(op.OperationType switch { "query" => "@queries", "mutation" => "@mutations", _ => "@subscriptions" }, op.Field, op.Field, $"{op.OperationType} {op.Field}"))
            .ToList();
        var roots = _roots.Values.ToHashSet(StringComparer.Ordinal);
        foreach (var type in _types.Values.Where(t => t.Kind != "scalar" && !roots.Contains(t.Name) && !t.Name.StartsWith("__", StringComparison.Ordinal)))
            entries.Add(new ApiCatalogEntry("@types", type.Name, type.Name, $"schema {type.Name}"));
        return entries;
    }

    public ApiOperation? GetOperation(string operationType, string fieldName)
    {
        if (!_roots.TryGetValue(operationType, out var rootName)
            || !_types.TryGetValue(rootName, out var root)
            || root.Fields.FirstOrDefault(f => f.Name == fieldName) is not { } field)
            return null;

        var arguments = field.Args.Select(ToArgField).ToList();
        var variables = new JsonObject();
        foreach (var arg in field.Args)
            variables[arg.Name] = ExampleFor(arg.Type, 0, []);

        var document = new StringBuilder();
        document.Append(operationType).Append(' ').Append(char.ToUpperInvariant(fieldName[0])).Append(fieldName[1..]);
        if (field.Args.Count > 0)
            document.Append('(').AppendJoin(", ", field.Args.Select(a => $"${a.Name}: {a.Type}")).Append(')');
        document.Append(" {\n  ").Append(fieldName);
        if (field.Args.Count > 0)
            document.Append('(').AppendJoin(", ", field.Args.Select(a => $"{a.Name}: ${a.Name}")).Append(')');
        document.Append(Selection(NamedType(field.Type), 1, [])).Append("\n}");

        var data = new JsonObject { [fieldName] = ExampleFor(field.Type, 0, []) };
        return new ApiOperation
        {
            Kind = ApiKind.GraphQl,
            Method = "POST",
            Path = "",
            Summary = fieldName,
            Description = field.Description,
            Deprecated = field.Deprecated,
            Parameters = arguments,
            Responses =
            [
                new ApiResponse("200", null, "application/json", [ToField(field)], new JsonObject { ["data"] = data }),
            ],
            GraphQlDocument = document.ToString(),
            GraphQlVariables = variables,
        };
    }

    public ApiObject? GetSchema(string name)
    {
        if (!_types.TryGetValue(name, out var type))
            return null;
        var fields = type.Kind == "enum"
            ? type.Values.Select(v => new ApiField { Name = v.Name, Type = "enum value", Description = v.Description }).ToList()
            : type.Fields.Select(ToField).ToList();
        return new ApiObject(name, type.Description, fields, type.Kind == "enum" ? null : ExampleFor(name, 0, []));
    }

    private ApiField ToArgField(Arg arg) => new()
    {
        Name = arg.Name,
        Type = arg.Type,
        Required = arg.Type.EndsWith('!') && arg.Default is null,
        Description = arg.Description,
        In = "argument",
        Default = arg.Default is null ? null : JsonValue.Create(arg.Default),
        Enum = EnumValues(arg.Type),
        Children = Children(arg.Type, 0, []),
    };

    private ApiField ToField(Field field) => ToField(field, 0, []);

    private ApiField ToField(Field field, int depth, HashSet<string> seen) => new()
    {
        Name = field.Name,
        Type = field.Type,
        Required = field.Type.EndsWith('!'),
        Description = field.Description,
        Deprecated = field.Deprecated,
        Enum = EnumValues(field.Type),
        Children = Children(field.Type, depth, seen),
    };

    private IReadOnlyList<ApiField> Children(string typeRef, int depth, HashSet<string> seen)
    {
        var name = NamedType(typeRef);
        if (depth >= MaxDepth || seen.Contains(name) || !_types.TryGetValue(name, out var type) || type.Fields.Count == 0)
            return [];
        var inner = new HashSet<string>(seen) { name };
        return type.Fields.Select(f => ToField(f, depth + 1, inner)).ToList();
    }

    private IReadOnlyList<string>? EnumValues(string typeRef) =>
        _types.TryGetValue(NamedType(typeRef), out var type) && type.Kind == "enum" ? type.Values.Select(v => v.Name).ToList() : null;

    private string Selection(string typeName, int depth, HashSet<string> seen)
    {
        if (!_types.TryGetValue(typeName, out var type) || type.Kind is "scalar" or "enum")
            return "";
        if (type.Kind == "union")
            return " {\n" + Indent(depth + 1) + "__typename\n" + Indent(depth) + "}";
        if (seen.Contains(typeName) || depth > MaxDepth)
            return " {\n" + Indent(depth + 1) + "__typename\n" + Indent(depth) + "}";

        var inner = new HashSet<string>(seen) { typeName };
        var lines = new List<string>();
        foreach (var field in type.Fields.Where(f => !f.Args.Any(a => a.Type.EndsWith('!') && a.Default is null)))
        {
            var named = NamedType(field.Type);
            var isLeaf = BuiltInScalars.Contains(named) || (_types.TryGetValue(named, out var t) && t.Kind is "scalar" or "enum");
            if (isLeaf)
                lines.Add(Indent(depth + 1) + field.Name);
            else if (depth < 2 && !inner.Contains(named))
                lines.Add(Indent(depth + 1) + field.Name + Selection(named, depth + 1, inner));
        }
        if (lines.Count == 0)
            lines.Add(Indent(depth + 1) + "__typename");
        return " {\n" + string.Join('\n', lines) + "\n" + Indent(depth) + "}";
    }

    private static string Indent(int depth) => new(' ', depth * 2);

    private JsonNode? ExampleFor(string typeRef, int depth, HashSet<string> seen)
    {
        var trimmed = typeRef.TrimEnd('!');
        if (trimmed.StartsWith('['))
            return ExampleFor(trimmed[1..^1], depth, seen) is { } item ? new JsonArray(item) : new JsonArray();

        switch (trimmed)
        {
            case "Int": return 123;
            case "Float": return 1.5;
            case "Boolean": return true;
            case "ID": return "<id>";
            case "String": return "<string>";
        }

        if (!_types.TryGetValue(trimmed, out var type))
            return "<" + trimmed + ">";
        if (type.Kind == "enum")
            return type.Values.Count > 0 ? type.Values[0].Name : null;
        if (type.Kind is "scalar" or "union" || depth > MaxDepth || seen.Contains(trimmed))
            return type.Kind == "union" ? new JsonObject { ["__typename"] = type.Members.FirstOrDefault() } : "<" + trimmed + ">";

        var inner = new HashSet<string>(seen) { trimmed };
        var obj = new JsonObject();
        foreach (var field in type.Fields)
            obj[field.Name] = ExampleFor(field.Type, depth + 1, inner);
        return obj;
    }

    private static string NamedType(string typeRef) => typeRef.Trim('[', ']', '!');

    private enum TokenKind { Name, Punct, String, Number, End }
    private readonly record struct Token(TokenKind Kind, string Value);

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c) || c == ',' || c == '﻿')
            {
                i++;
            }
            else if (c == '#')
            {
                while (i < text.Length && text[i] != '\n')
                    i++;
            }
            else if (c == '"')
            {
                if (string.CompareOrdinal(text, i, "\"\"\"", 0, 3) == 0)
                {
                    var end = text.IndexOf("\"\"\"", i + 3, StringComparison.Ordinal);
                    while (end > 0 && text[end - 1] == '\\')
                        end = text.IndexOf("\"\"\"", end + 3, StringComparison.Ordinal);
                    if (end < 0)
                        throw new FormatException("Unterminated block string.");
                    tokens.Add(new Token(TokenKind.String, BlockString(text[(i + 3)..end])));
                    i = end + 3;
                }
                else
                {
                    var sb = new StringBuilder();
                    i++;
                    while (i < text.Length && text[i] != '"')
                    {
                        if (text[i] == '\\' && i + 1 < text.Length)
                        {
                            i++;
                            sb.Append(text[i] switch { 'n' => '\n', 't' => '\t', _ => text[i] });
                        }
                        else
                        {
                            sb.Append(text[i]);
                        }
                        i++;
                    }
                    tokens.Add(new Token(TokenKind.String, sb.ToString()));
                    i++;
                }
            }
            else if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                    i++;
                tokens.Add(new Token(TokenKind.Name, text[start..i]));
            }
            else if (char.IsDigit(c) || c == '-')
            {
                var start = i++;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '.' or '+' or '-'))
                    i++;
                tokens.Add(new Token(TokenKind.Number, text[start..i]));
            }
            else if (string.CompareOrdinal(text, i, "...", 0, 3) == 0)
            {
                tokens.Add(new Token(TokenKind.Punct, "..."));
                i += 3;
            }
            else
            {
                tokens.Add(new Token(TokenKind.Punct, c.ToString()));
                i++;
            }
        }
        tokens.Add(new Token(TokenKind.End, ""));
        return tokens;
    }

    private static string BlockString(string raw)
    {
        var lines = raw.Replace("\\\"\"\"", "\"\"\"").Split('\n');
        var indent = lines.Skip(1).Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
        return string.Join('\n', lines.Select((l, n) => n == 0 ? l : l[Math.Min(indent, l.Length)..])).Trim();
    }

    private sealed class Parser(List<Token> tokens, GraphQlReader reader)
    {
        private static readonly HashSet<string> Keywords = ["type", "interface", "input", "enum", "union", "scalar", "schema", "directive", "extend"];
        private int _pos;

        private Token Peek => tokens[_pos];
        private Token Next() => tokens[_pos++];
        private bool IsPunct(string p) => Peek.Kind == TokenKind.Punct && Peek.Value == p;

        private void Expect(string p)
        {
            if (!IsPunct(p))
                throw new FormatException($"Expected '{p}', found '{Peek.Value}'.");
            _pos++;
        }

        private string Name() => Peek.Kind == TokenKind.Name ? Next().Value : throw new FormatException($"Expected a name, found '{Peek.Value}'.");

        private string? Description() => Peek.Kind == TokenKind.String ? Next().Value : null;

        public void ParseDocument()
        {
            while (Peek.Kind != TokenKind.End)
            {
                var description = Description();
                var keyword = Name();
                var extend = keyword == "extend";
                if (extend)
                    keyword = Name();

                switch (keyword)
                {
                    case "schema":
                        SkipDirectives(out _);
                        Expect("{");
                        while (!IsPunct("}"))
                        {
                            var operation = Name();
                            Expect(":");
                            reader._roots[operation] = Name();
                        }
                        Expect("}");
                        break;
                    case "type" or "interface" or "input":
                        ParseObject(keyword, description);
                        break;
                    case "enum":
                        ParseEnum(description);
                        break;
                    case "union":
                        var union = Get("union", Name(), description);
                        SkipDirectives(out _);
                        if (IsPunct("="))
                        {
                            Next();
                            if (IsPunct("|"))
                                Next();
                            union.Members.Add(Name());
                            while (IsPunct("|"))
                            {
                                Next();
                                union.Members.Add(Name());
                            }
                        }
                        break;
                    case "scalar":
                        Get("scalar", Name(), description);
                        SkipDirectives(out _);
                        break;
                    case "directive":
                        Expect("@");
                        Name();
                        if (IsPunct("("))
                            ParseArgs();
                        if (Peek.Value == "repeatable")
                            Next();
                        if (Name() != "on")
                            throw new FormatException("Expected 'on' in directive definition.");
                        if (IsPunct("|"))
                            Next();
                        Name();
                        while (IsPunct("|"))
                        {
                            Next();
                            Name();
                        }
                        break;
                    default:
                        throw new FormatException($"Unknown definition '{keyword}'.");
                }
            }
        }

        private TypeDef Get(string kind, string name, string? description)
        {
            if (!reader._types.TryGetValue(name, out var type))
                reader._types[name] = type = new TypeDef(kind, name);
            type.Description ??= description;
            return type;
        }

        private void ParseObject(string kind, string? description)
        {
            var type = Get(kind, Name(), description);
            if (Peek.Value == "implements")
            {
                Next();
                if (IsPunct("&"))
                    Next();
                Name();
                while (IsPunct("&") || (Peek.Kind == TokenKind.Name && !Keywords.Contains(Peek.Value)))
                {
                    if (IsPunct("&"))
                        Next();
                    Name();
                }
            }
            SkipDirectives(out _);
            if (!IsPunct("{"))
                return;
            Next();
            while (!IsPunct("}"))
            {
                var fieldDescription = Description();
                var name = Name();
                var args = IsPunct("(") ? ParseArgs() : [];
                Expect(":");
                var fieldType = TypeRef();
                if (IsPunct("="))
                {
                    Next();
                    Value();
                }
                type.Fields.Add(new Field(name, fieldType, fieldDescription, args, SkipDirectivesDeprecated()));
            }
            Expect("}");
        }

        private void ParseEnum(string? description)
        {
            var type = Get("enum", Name(), description);
            SkipDirectives(out _);
            if (!IsPunct("{"))
                return;
            Next();
            while (!IsPunct("}"))
            {
                var valueDescription = Description();
                var name = Name();
                SkipDirectives(out _);
                type.Values.Add((name, valueDescription));
            }
            Expect("}");
        }

        private List<Arg> ParseArgs()
        {
            var args = new List<Arg>();
            Expect("(");
            while (!IsPunct(")"))
            {
                var description = Description();
                var name = Name();
                Expect(":");
                var type = TypeRef();
                string? defaultValue = null;
                if (IsPunct("="))
                {
                    Next();
                    defaultValue = Value();
                }
                SkipDirectives(out _);
                args.Add(new Arg(name, type, description, defaultValue));
            }
            Expect(")");
            return args;
        }

        private string TypeRef()
        {
            string type;
            if (IsPunct("["))
            {
                Next();
                type = "[" + TypeRef() + "]";
                Expect("]");
            }
            else
            {
                type = Name();
            }
            if (IsPunct("!"))
            {
                Next();
                type += "!";
            }
            return type;
        }

        private string Value()
        {
            var token = Next();
            if (token.Kind == TokenKind.Punct && token.Value is "[" or "{")
            {
                var close = token.Value == "[" ? "]" : "}";
                var sb = new StringBuilder(token.Value);
                while (!IsPunct(close))
                {
                    if (Peek.Kind == TokenKind.End)
                        throw new FormatException("Unterminated value.");
                    sb.Append(' ').Append(Value());
                }
                Next();
                return sb.Append(' ').Append(close).ToString();
            }
            return token.Kind == TokenKind.String ? "\"" + token.Value + "\"" : token.Value;
        }

        private bool SkipDirectivesDeprecated()
        {
            SkipDirectives(out var deprecated);
            return deprecated;
        }

        private void SkipDirectives(out bool deprecated)
        {
            deprecated = false;
            while (IsPunct("@"))
            {
                Next();
                if (Name() == "deprecated")
                    deprecated = true;
                if (!IsPunct("("))
                    continue;
                var depth = 0;
                do
                {
                    var token = Next();
                    if (token.Kind == TokenKind.End)
                        throw new FormatException("Unterminated directive arguments.");
                    if (token.Kind == TokenKind.Punct && token.Value == "(")
                        depth++;
                    else if (token.Kind == TokenKind.Punct && token.Value == ")")
                        depth--;
                } while (depth > 0);
            }
        }
    }
}
