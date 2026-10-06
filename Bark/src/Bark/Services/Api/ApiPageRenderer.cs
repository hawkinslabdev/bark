using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bark.Models;
using Bark.Services.Rendering;

namespace Bark.Services.Api;

/// <summary>Renders an API operation or object page: reference sections, request samples, response examples and playground data.</summary>
public static partial class ApiPageRenderer
{
    public const string RootClass = "bark-api";

    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // Default encoder escapes <, >, & and ' so the payload cannot close its <script> element.
    private static readonly JsonSerializerOptions Embedded = new() { Encoder = JavaScriptEncoder.Default };

    private static readonly string[] PlaygroundLabelKeys =
    [
        "apiSend", "apiSending", "apiClose", "apiServer", "apiAuthorizations", "apiPathParameters", "apiQueryParameters",
        "apiHeaders", "apiCookies", "apiQueryString", "apiArguments", "apiBody", "apiResponse", "apiVariables", "apiQuery",
        "apiResponseEmpty", "apiRequestFailed", "apiInvalidJson", "apiCookieBlocked", "apiResponseHeaders", "apiCredentialsNote",
        "apiRequired", "apiTime", "apiSize",
    ];

    public sealed record Options(Localization Localization, Func<string, string> MarkdownToHtml, string? Title, string? ServerOverride, string? AuthOverride);

    public static string Render(ApiPageModel model, string bodyHtml, Options options)
    {
        if (model.Object is { } obj)
            return RenderObject(obj, bodyHtml, options);
        return RenderOperation(ApplyOverrides(model.Operation!, options), bodyHtml, options);
    }

    /// <summary>Title for a page that sets no <c>title</c>: the operation summary, object name or method and path.</summary>
    public static string DefaultTitle(ApiPageModel model) =>
        model.Object?.Name
        ?? model.Operation!.Summary
        ?? $"{model.Operation.Method} {model.Operation.Path}";

    /// <summary>Absolute http(s) origins of the operation servers, for the page connect-src directive.</summary>
    public static IReadOnlyList<string> Origins(ApiPageModel model, string? serverOverride)
    {
        if (model.Operation is null || model.Operation.Webhook)
            return [];
        var servers = serverOverride is { Length: > 0 } ? [serverOverride] : model.Operation.Servers;
        return servers
            // Scheme and authority only: userinfo ("user;x@host") must never reach the CSP header.
            .Select(s => Uri.TryCreate(s, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" ? $"{uri.Scheme}://{uri.Authority}" : null)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ApiOperation ApplyOverrides(ApiOperation op, Options options)
    {
        if (options.ServerOverride is { Length: > 0 } server)
            op = op with { Servers = [server.TrimEnd('/')] };
        if (options.AuthOverride is { Length: > 0 } auth && ParseAuth(auth) is { } parsed)
            op = op with { Auth = [parsed] };
        return op;
    }

    // Front matter auth: "bearer", "basic", or "apiKey <header|query> <name>".
    private static ApiAuth? ParseAuth(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts[0].ToLowerInvariant() switch
        {
            "bearer" => new ApiAuth("bearer", "Authorization", "header", null),
            "basic" => new ApiAuth("basic", "Authorization", "header", null),
            "apikey" when parts.Length >= 3 => new ApiAuth("apiKey", parts[2], parts[1].ToLowerInvariant(), null),
            _ => null,
        };
    }

    private static string RenderOperation(ApiOperation op, string bodyHtml, Options options)
    {
        var l = options.Localization;
        var md = options.MarkdownToHtml;
        var server = op.Servers.FirstOrDefault() ?? "";
        var title = options.Title ?? op.Summary ?? $"{op.Method} {op.Path}";
        var canInvoke = !op.Webhook && !(op.Kind == ApiKind.GraphQl && op.GraphQlDocument?.StartsWith("subscription", StringComparison.Ordinal) == true);
        var sb = new StringBuilder();

        sb.Append("<div class=\"").Append(RootClass).Append("\" data-api-kind=\"").Append(op.Kind.ToString().ToLowerInvariant()).Append("\">");
        sb.Append("<div class=\"api-columns\"><div class=\"api-main\">");
        sb.Append("<h1>").Append(Encode(title)).Append("</h1>");
        if (op.Description is { Length: > 0 } description)
            sb.Append("<div class=\"api-lead\">").Append(md(description)).Append("</div>");

        sb.Append("<div class=\"api-endpoint\" title=\"").Append(Encode(server + op.Path)).Append("\">");
        sb.Append(MethodBadge(op.Webhook ? l.Text("apiWebhook") : op.Method, op.Webhook ? "webhook" : op.Method));
        sb.Append("<code class=\"api-endpoint-path\">").Append(Encode(op.Path.Length > 0 ? op.Path : server)).Append("</code>");
        if (canInvoke)
        {
            sb.Append("<button type=\"button\" class=\"api-try\" data-api-try>").Append(Encode(l.Text("apiTryIt")))
              .Append("<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M8 5v14l11-7z\" fill=\"currentColor\"/></svg></button>");
        }
        sb.Append("</div>");

        if (op.Deprecated)
            sb.Append("<div class=\"warning custom-block\"><p class=\"custom-block-title\">").Append(Encode(l.Text("apiDeprecated"))).Append("</p></div>");
        if (bodyHtml.Length > 0)
            sb.Append("<div class=\"api-body\">").Append(bodyHtml).Append("</div>");

        if (op.Auth.Count > 0)
        {
            var authFields = op.Auth.Select(a => new ApiField
            {
                Name = a.Name,
                Type = "string",
                In = a.In,
                Required = true,
                Description = a.Description ?? a.Kind switch
                {
                    "bearer" => l.Text("apiBearerDescription"),
                    "basic" => l.Text("apiBasicDescription"),
                    _ => null,
                },
            }).ToList();
            Section(sb, "authorizations", l.Text("apiAuthorizations"), null, authFields, md, l, showLocation: true);
        }

        foreach (var (location, key) in new[] { ("path", "apiPathParameters"), ("query", "apiQueryParameters"), ("querystring", "apiQueryString"), ("header", "apiHeaders"), ("cookie", "apiCookies"), ("argument", "apiArguments") })
        {
            var fields = op.Parameters.Where(p => p.In == location).ToList();
            if (fields.Count > 0)
                Section(sb, Slug(location + "-parameters"), l.Text(key), null, fields, md, l);
        }

        if (op.Body is { } body)
            Section(sb, "body", l.Text("apiBody"), Encode(body.ContentType), body.Fields, md, l);

        if (op.Responses.Count > 0)
        {
            var meta = new StringBuilder();
            if (op.Responses.Count > 1)
            {
                meta.Append("<select class=\"api-status-select\" data-api-status aria-label=\"").Append(Encode(l.Text("apiStatusCode"))).Append("\">");
                foreach (var response in op.Responses)
                    meta.Append("<option value=\"").Append(Encode(response.Status)).Append("\">").Append(Encode(response.Status)).Append("</option>");
                meta.Append("</select>");
            }
            else
            {
                meta.Append("<span class=\"api-status-label\">").Append(Encode(op.Responses[0].Status)).Append("</span>");
            }
            meta.Append("<span class=\"api-content-type\" data-api-content-type>").Append(Encode(op.Responses[0].ContentType ?? "")).Append("</span>");

            sb.Append("<section class=\"api-section\">");
            SectionHead(sb, "response", l.Text("apiResponse"), meta.ToString());
            var first = true;
            foreach (var response in op.Responses)
            {
                var id = "response-" + Slug(response.Status);
                sb.Append("<div class=\"api-response\" id=\"").Append(id).Append("\" data-status=\"").Append(Encode(response.Status))
                  .Append("\" data-content-type=\"").Append(Encode(response.ContentType ?? "")).Append('"');
                if (!first)
                    sb.Append(" data-api-inactive");
                sb.Append('>');
                if (op.Responses.Count > 1)
                    sb.Append("<p class=\"api-response-status\"><span class=\"api-status\" data-status=\"").Append(Encode(response.Status[..1])).Append("\">").Append(Encode(response.Status)).Append("</span></p>");
                if (response.Description is { Length: > 0 } responseDescription)
                    sb.Append("<div class=\"api-response-description\">").Append(md(responseDescription)).Append("</div>");
                Fields(sb, response.Fields, id, md, l, 0, false);
                sb.Append("</div>");
                first = false;
            }
            sb.Append("</section>");
        }
        sb.Append("</div>");

        sb.Append("<aside class=\"api-aside\" aria-label=\"").Append(Encode(l.Text("apiExamplesAria"))).Append("\">");
        if (!op.Webhook)
            sb.Append(SamplePanel(l.Text("apiRequestExample"), RequestSamples(op, server), md, l));
        else if (op.Body?.Example is { } payload)
            sb.Append(SamplePanel(l.Text("apiPayload"), [("JSON", "json", ToJson(payload))], md, l));
        var responseExamples = op.Responses.Where(r => r.Example is not null).ToList();
        if (responseExamples.Count > 0)
            sb.Append(StatusPanel(responseExamples, md, l));
        sb.Append("</aside></div>");

        if (canInvoke)
            sb.Append("<script type=\"application/json\" class=\"bark-api-data\">").Append(PlaygroundData(op, l)).Append("</script>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderObject(ApiObject obj, string bodyHtml, Options options)
    {
        var l = options.Localization;
        var sb = new StringBuilder();
        sb.Append("<div class=\"").Append(RootClass).Append("\" data-api-kind=\"object\">");
        sb.Append("<div class=\"api-columns\"><div class=\"api-main\">");
        sb.Append("<h1>").Append(Encode(options.Title ?? obj.Name)).Append("</h1>");
        if (obj.Description is { Length: > 0 } description)
            sb.Append("<div class=\"api-lead\">").Append(options.MarkdownToHtml(description)).Append("</div>");
        if (bodyHtml.Length > 0)
            sb.Append("<div class=\"api-body\">").Append(bodyHtml).Append("</div>");
        Section(sb, "attributes", l.Text("apiAttributes"), null, obj.Fields, options.MarkdownToHtml, l);
        sb.Append("</div>");
        if (obj.Example is not null)
        {
            sb.Append("<aside class=\"api-aside\" aria-label=\"").Append(Encode(l.Text("apiExamplesAria"))).Append("\">")
              .Append(SamplePanel(obj.Name, [("JSON", "json", ToJson(obj.Example))], options.MarkdownToHtml, l))
              .Append("</aside>");
        }
        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static void SectionHead(StringBuilder sb, string id, string heading, string? metaHtml)
    {
        sb.Append("<div class=\"api-section-head\"><h2 id=\"").Append(id).Append("\">").Append(Encode(heading)).Append("</h2>");
        if (metaHtml is { Length: > 0 })
            sb.Append("<div class=\"api-section-meta\">").Append(metaHtml).Append("</div>");
        sb.Append("</div>");
    }

    private static void Section(StringBuilder sb, string id, string heading, string? metaHtml, IReadOnlyList<ApiField> fields, Func<string, string> md, Localization l, bool showLocation = false)
    {
        sb.Append("<section class=\"api-section\">");
        SectionHead(sb, id, heading, metaHtml is null ? null : $"<span class=\"api-content-type\">{metaHtml}</span>");
        Fields(sb, fields, id, md, l, 0, showLocation);
        sb.Append("</section>");
    }

    private static void Fields(StringBuilder sb, IReadOnlyList<ApiField> fields, string idPrefix, Func<string, string> md, Localization l, int depth, bool showLocation)
    {
        if (fields.Count == 0)
            return;
        sb.Append("<div class=\"api-fields\">");
        foreach (var field in fields)
        {
            var id = idPrefix + "-" + Slug(field.Name);
            sb.Append("<div class=\"api-field\" id=\"").Append(id).Append("\"><div class=\"api-field-head\">")
              .Append("<a class=\"api-field-name\" href=\"#").Append(id).Append("\">").Append(Encode(field.Name)).Append("</a>")
              .Append("<span class=\"api-field-type\">").Append(Encode(field.Type)).Append("</span>");
            if (showLocation && field.In is { } location)
                sb.Append("<span class=\"api-field-flag\">").Append(Encode(location)).Append("</span>");
            if (field.ReadOnly)
                sb.Append("<span class=\"api-field-flag\">").Append(Encode(l.Text("apiReadOnly"))).Append("</span>");
            if (field.Deprecated)
                sb.Append("<span class=\"api-field-flag api-field-deprecated\">").Append(Encode(l.Text("apiDeprecated"))).Append("</span>");
            if (field.Required)
                sb.Append("<span class=\"api-field-flag api-field-required\">").Append(Encode(l.Text("apiRequired"))).Append("</span>");
            sb.Append("</div>");

            var hasBody = field.Description is { Length: > 0 } || field.Enum is { Count: > 0 } || field.Default is not null || (field.Example is not null && field.Children.Count == 0);
            if (hasBody)
            {
                sb.Append("<div class=\"api-field-body\">");
                if (field.Description is { Length: > 0 } description)
                    sb.Append(md(description));
                if (field.Enum is { Count: > 0 } values)
                {
                    sb.Append("<p class=\"api-field-meta\">").Append(Encode(l.Text("apiAllowed"))).Append(' ')
                      .AppendJoin(" ", values.Select(v => "<code>" + Encode(v) + "</code>")).Append("</p>");
                }
                var facts = new List<string>();
                if (field.Default is not null)
                    facts.Add($"{Encode(l.Text("apiDefault"))} <code>{Encode(Scalar(field.Default))}</code>");
                if (field.Example is not null && field.Children.Count == 0)
                    facts.Add($"{Encode(l.Text("apiExample"))} <code>{Encode(Scalar(field.Example))}</code>");
                if (facts.Count > 0)
                    sb.Append("<p class=\"api-field-meta\">").AppendJoin("<span class=\"api-sep\" aria-hidden=\"true\"></span>", facts).Append("</p>");
                sb.Append("</div>");
            }

            if (field.Children.Count > 0)
            {
                sb.Append("<details class=\"api-children\"><summary>")
                  .Append("<svg class=\"api-chevron\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M9 6l6 6-6 6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>")
                  .Append("<span class=\"api-children-show\">").Append(Encode(l.Text("apiShowChildren"))).Append("</span>")
                  .Append("<span class=\"api-children-hide\">").Append(Encode(l.Text("apiHideChildren"))).Append("</span>")
                  .Append("</summary>");
                Fields(sb, field.Children, id, md, l, depth + 1, false);
                sb.Append("</details>");
            }
            sb.Append("</div>");
        }
        sb.Append("</div>");
    }

    private static string MethodBadge(string label, string method) =>
        $"<span class=\"api-method\" data-method=\"{Encode(method.ToLowerInvariant())}\">{Encode(label.ToUpperInvariant())}</span>";

    private static readonly Dictionary<string, string> SampleIcons = new(StringComparer.Ordinal)
    {
        ["cURL"] = "bash",
        ["Python"] = "python",
        ["JavaScript"] = "javascript",
        ["PHP"] = "php",
        ["Go"] = "go",
    };

    // Card with a title and a language dropdown; one highlighted block per language.
    private static string SamplePanel(string title, IReadOnlyList<(string Title, string Lang, string Code)> samples, Func<string, string> md, Localization l)
    {
        var sb = new StringBuilder("<div class=\"api-panel\" data-api-samples>");
        sb.Append("<div class=\"api-panel-head\"><span class=\"api-panel-title\">").Append(Encode(title)).Append("</span><div class=\"api-panel-tools\">");
        if (samples.Count > 1)
        {
            sb.Append("<label class=\"api-lang\"><span class=\"sr-only\">").Append(Encode(l.Text("apiLanguage"))).Append("</span>")
              .Append("<select data-api-lang>");
            foreach (var (name, _, _) in samples)
            {
                sb.Append("<option value=\"").Append(Encode(name)).Append('"');
                if (SampleIcons.TryGetValue(name, out var icon))
                    sb.Append(" data-icon=\"").Append(icon).Append('"');
                sb.Append('>').Append(Encode(name)).Append("</option>");
            }
            sb.Append("</select></label>");
        }
        sb.Append("</div></div><div class=\"api-panel-body\">");
        var first = true;
        foreach (var (name, lang, code) in samples)
        {
            sb.Append("<div class=\"api-sample\" data-sample=\"").Append(Encode(name)).Append('"');
            if (!first)
                sb.Append(" hidden");
            sb.Append('>').Append(md(Fence(lang, code))).Append("</div>");
            first = false;
        }
        return sb.Append("</div></div>").ToString();
    }

    // Card with one tab per status code; the Response section dropdown selects the same tab.
    private static string StatusPanel(IReadOnlyList<ApiResponse> responses, Func<string, string> md, Localization l)
    {
        var sb = new StringBuilder("<div class=\"api-panel\" data-api-responses>");
        sb.Append("<div class=\"api-panel-head\"><span class=\"api-panel-title\">").Append(Encode(l.Text("apiResponseExample"))).Append("</span><div class=\"api-panel-tools\">");
        if (responses.Count > 1)
        {
            sb.Append("<div class=\"api-status-tabs\" role=\"tablist\" aria-label=\"").Append(Encode(l.Text("apiStatusCode"))).Append("\">");
            var selected = true;
            foreach (var response in responses)
            {
                sb.Append("<button type=\"button\" role=\"tab\" class=\"api-status-tab\" data-status=\"").Append(Encode(response.Status))
                  .Append("\" aria-selected=\"").Append(selected ? "true" : "false").Append("\"").Append(selected ? "" : " tabindex=\"-1\"").Append('>')
                  .Append(Encode(response.Status)).Append("</button>");
                selected = false;
            }
            sb.Append("</div>");
        }
        else
        {
            sb.Append("<span class=\"api-status\" data-status=\"").Append(Encode(responses[0].Status[..1])).Append("\">").Append(Encode(responses[0].Status)).Append("</span>");
        }
        sb.Append("</div></div><div class=\"api-panel-body\">");
        var first = true;
        first = true;
        foreach (var response in responses)
        {
            sb.Append("<div class=\"api-sample\" role=\"tabpanel\" data-sample=\"").Append(Encode(response.Status)).Append('"');
            if (!first)
                sb.Append(" hidden");
            sb.Append('>').Append(md(Fence("json", ToJson(response.Example)))).Append("</div>");
            first = false;
        }
        return sb.Append("</div></div>").ToString();
    }

    private static string Fence(string lang, string code)
    {
        var fence = new string('`', Math.Max(3, LongestBacktickRun(code) + 1));
        return $"{fence}{lang}\n{code.TrimEnd()}\n{fence}\n";
    }

    private static int LongestBacktickRun(string text) =>
        BacktickRunRegex().Matches(text).Select(m => m.Length).DefaultIfEmpty(0).Max();

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();

    private sealed record Request(string Method, string Url, List<(string Name, string Value)> Headers, string? Json, List<(string Name, string Value, bool File)>? Form, bool Multipart)
    {
        /// <summary>Structured JSON body, for languages that send a native object.</summary>
        public JsonNode? Data { get; init; }
    }

    private static Request BuildRequest(ApiOperation op, string server)
    {
        var url = server + op.Path;
        var query = new List<string>();
        var headers = new List<(string, string)>();

        foreach (var auth in op.Auth)
        {
            var placeholder = auth.Kind switch
            {
                "bearer" => "Bearer <token>",
                "basic" => "Basic <encoded-value>",
                _ => "<api-key>",
            };
            if (auth.In == "query")
                query.Add($"{auth.Name}=<api-key>");
            else if (auth.In == "header")
                headers.Add((auth.Name, placeholder));
        }

        foreach (var p in op.Parameters)
        {
            var value = Scalar(p.Example ?? p.Default ?? (p.Enum is { Count: > 0 } e ? JsonValue.Create(e[0]) : null)) is { Length: > 0 } v ? v : $"<{p.Name}>";
            switch (p.In)
            {
                case "query" when p.Required || p.Example is not null:
                    query.Add($"{p.Name}={value}");
                    break;
                case "querystring":
                    query.Add(value);
                    break;
                case "header":
                    headers.Add((p.Name, value));
                    break;
            }
        }
        if (query.Count > 0)
            url += (url.Contains('?') ? "&" : "?") + string.Join('&', query);

        string? json = null;
        JsonNode? data = null;
        List<(string, string, bool)>? form = null;
        var multipart = false;
        if (op.Kind == ApiKind.GraphQl)
        {
            headers.Add(("Content-Type", "application/json"));
            data = new JsonObject { ["query"] = op.GraphQlDocument, ["variables"] = op.GraphQlVariables?.DeepClone() };
            json = ToJson(data);
        }
        else if (op.Body is { } body)
        {
            multipart = body.ContentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase);
            if (multipart || body.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                form = body.Fields.Select(f => (f.Name, Scalar(f.Example ?? f.Default) is { Length: > 0 } v ? v : $"<{f.Name}>", f.Type.Contains("binary") || f.Type == "file")).ToList();
                if (!multipart)
                    headers.Add(("Content-Type", body.ContentType));
            }
            else
            {
                headers.Add(("Content-Type", body.ContentType));
                json = body.Example is JsonValue text && text.TryGetValue<string>(out var raw) && !body.ContentType.Contains("json") ? raw : ToJson(body.Example);
                if (body.ContentType.Contains("json") && body.Example is JsonObject or JsonArray)
                    data = body.Example;
            }
        }
        return new Request(op.Method, url, headers, json, form, multipart) { Data = data };
    }

    private static List<(string, string, string)> RequestSamples(ApiOperation op, string server)
    {
        var r = BuildRequest(op, server);
        return
        [
            ("cURL", "bash", Curl(r)),
            ("Python", "python", Python(r)),
            ("JavaScript", "javascript", JavaScript(r)),
            ("PHP", "php", Php(r)),
            ("Go", "go", Go(r)),
            ("Java", "java", Java(r)),
        ];
    }

    private static string FormEncoded(Request r) => string.Join('&', r.Form!.Select(f => $"{f.Name}={f.Value}"));

    private static string? PayloadText(Request r) => r.Json ?? (r.Form is null ? null : FormEncoded(r));

    private static string Curl(Request r)
    {
        var sb = new StringBuilder($"curl --request {r.Method} \\\n  --url '{r.Url}'");
        foreach (var (name, value) in r.Headers)
            sb.Append($" \\\n  --header '{name}: {value}'");
        if (r.Form is not null)
        {
            foreach (var (name, value, file) in r.Form)
                sb.Append(r.Multipart ? $" \\\n  --form '{name}={(file ? "@" : "")}{value}'" : $" \\\n  --data-urlencode '{name}={value}'");
        }
        else if (r.Json is not null)
        {
            sb.Append(" \\\n  --data '").Append(r.Json.Replace("'", "'\\''")).Append('\'');
        }
        return sb.ToString();
    }

    private static string Python(Request r)
    {
        var sb = new StringBuilder("import requests\n\n");
        sb.Append($"url = {Quote(r.Url)}\n\n");
        string? argument = null;
        if (r.Data is not null)
        {
            sb.Append("payload = ").Append(Literal(r.Data, 0, python: true)).Append('\n');
            argument = "json=payload";
        }
        else if (r.Json is not null)
        {
            sb.Append($"payload = {Quote(r.Json)}\n");
            argument = "data=payload";
        }
        else if (r.Form is not null)
        {
            sb.Append("payload = {").AppendJoin(", ", r.Form.Where(f => !f.File).Select(f => $"{Quote(f.Name)}: {Quote(f.Value)}")).Append("}\n");
            argument = "data=payload";
        }
        if (r.Form?.Any(f => f.File) == true)
            sb.Append("files = {").AppendJoin(", ", r.Form.Where(f => f.File).Select(f => $"{Quote(f.Name)}: open({Quote(f.Value)}, \"rb\")")).Append("}\n");
        var headers = r.Data is null ? r.Headers : r.Headers.Where(h => h.Name != "Content-Type").ToList();
        sb.Append("headers = {").AppendJoin(", ", headers.Select(h => $"{Quote(h.Name)}: {Quote(h.Value)}")).Append("}\n\n");
        sb.Append($"response = requests.request({Quote(r.Method)}, url");
        if (argument is not null)
            sb.Append(", ").Append(argument);
        if (r.Form?.Any(f => f.File) == true)
            sb.Append(", files=files");
        sb.Append(", headers=headers)\n\nprint(response.text)");
        return sb.ToString();
    }

    // Python dict/list or PHP array literal for a JSON value.
    private static string Literal(JsonNode? node, int depth, bool python)
    {
        var pad = new string(' ', (depth + 1) * 4);
        var end = new string(' ', depth * 4);
        switch (node)
        {
            case null:
                return python ? "None" : "null";
            case JsonObject obj when obj.Count == 0:
                return python ? "{}" : "[]";
            case JsonObject obj:
                var entries = obj.Select(kv => pad + PhpOrPyString(kv.Key, python) + (python ? ": " : " => ") + Literal(kv.Value, depth + 1, python));
                return (python ? "{" : "[") + "\n" + string.Join(",\n", entries) + "\n" + end + (python ? "}" : "]");
            case JsonArray arr when arr.Count == 0:
                return "[]";
            case JsonArray arr:
                return "[\n" + string.Join(",\n", arr.Select(v => pad + Literal(v, depth + 1, python))) + "\n" + end + "]";
            case JsonValue value when value.TryGetValue<bool>(out var b):
                return python ? (b ? "True" : "False") : (b ? "true" : "false");
            case JsonValue value when value.TryGetValue<string>(out var s):
                return PhpOrPyString(s, python);
            default:
                return node.ToJsonString();
        }
    }

    private static string PhpOrPyString(string value, bool python) =>
        python ? Quote(value) : "'" + value.Replace("\\", "\\\\").Replace("'", "\\'") + "'";

    private static string JavaScript(Request r)
    {
        var sb = new StringBuilder();
        if (r.Form is not null && r.Multipart)
        {
            sb.Append("const form = new FormData();\n");
            foreach (var (name, value, file) in r.Form)
                sb.Append($"form.append({Quote(name)}, {(file ? "fileInput.files[0]" : Quote(value))});\n");
            sb.Append('\n');
        }
        sb.Append($"const options = {{\n  method: {Quote(r.Method)},\n  headers: {{");
        sb.AppendJoin(", ", r.Headers.Select(h => $"{Quote(h.Name)}: {Quote(h.Value)}")).Append('}');
        if (r.Form is not null && r.Multipart)
            sb.Append(",\n  body: form");
        else if (PayloadText(r) is { } payload)
            sb.Append($",\n  body: {(r.Json is not null ? $"JSON.stringify({r.Json.Replace("\n", "\n  ")})" : Quote(payload))}");
        sb.Append($"\n}};\n\nfetch({Quote(r.Url)}, options)\n  .then(res => res.json())\n  .then(res => console.log(res))\n  .catch(err => console.error(err));");
        return sb.ToString();
    }

    private static string Php(Request r)
    {
        var sb = new StringBuilder("<?php\n\n$curl = curl_init();\n\ncurl_setopt_array($curl, [\n");
        sb.Append($"  CURLOPT_URL => {Quote(r.Url)},\n  CURLOPT_RETURNTRANSFER => true,\n  CURLOPT_CUSTOMREQUEST => {Quote(r.Method)},\n");
        if (r.Data is not null)
            sb.Append("  CURLOPT_POSTFIELDS => json_encode(").Append(Literal(r.Data, 1, python: false)).Append("),\n");
        else if (PayloadText(r) is { } payload)
            sb.Append($"  CURLOPT_POSTFIELDS => {Quote(payload)},\n");
        sb.Append("  CURLOPT_HTTPHEADER => [").AppendJoin(", ", r.Headers.Select(h => Quote($"{h.Name}: {h.Value}"))).Append("],\n]);\n\n");
        sb.Append("$response = curl_exec($curl);\n$err = curl_error($curl);\n\ncurl_close($curl);\n\nif ($err) {\n  echo \"cURL Error #:\" . $err;\n} else {\n  echo $response;\n}");
        return sb.ToString();
    }

    private static string Go(Request r)
    {
        var payload = PayloadText(r);
        var sb = new StringBuilder("package main\n\nimport (\n\t\"fmt\"\n\t\"io\"\n\t\"net/http\"\n");
        if (payload is not null)
            sb.Append("\t\"strings\"\n");
        sb.Append(")\n\nfunc main() {\n\n\turl := ").Append(Quote(r.Url)).Append("\n\n");
        if (payload is not null)
            sb.Append("\tpayload := strings.NewReader(").Append(Quote(payload)).Append(")\n\n");
        sb.Append($"\treq, _ := http.NewRequest({Quote(r.Method)}, url, {(payload is null ? "nil" : "payload")})\n\n");
        foreach (var (name, value) in r.Headers)
            sb.Append($"\treq.Header.Add({Quote(name)}, {Quote(value)})\n");
        sb.Append("\n\tres, _ := http.DefaultClient.Do(req)\n\n\tdefer res.Body.Close()\n\tbody, _ := io.ReadAll(res.Body)\n\n\tfmt.Println(string(body))\n\n}");
        return sb.ToString();
    }

    private static string Java(Request r)
    {
        var payload = PayloadText(r);
        var sb = new StringBuilder($"HttpRequest request = HttpRequest.newBuilder()\n    .uri(URI.create({Quote(r.Url)}))\n");
        foreach (var (name, value) in r.Headers)
            sb.Append($"    .header({Quote(name)}, {Quote(value)})\n");
        sb.Append($"    .method({Quote(r.Method)}, {(payload is null ? "HttpRequest.BodyPublishers.noBody()" : $"HttpRequest.BodyPublishers.ofString({Quote(payload)})")})\n    .build();\n");
        sb.Append("HttpResponse<String> response = HttpClient.newHttpClient().send(request, HttpResponse.BodyHandlers.ofString());\nSystem.out.println(response.body());");
        return sb.ToString();
    }

    private static string Quote(string value) => JsonSerializer.Serialize(value, Pretty);

    private static string PlaygroundData(ApiOperation op, Localization l)
    {
        var data = new JsonObject
        {
            ["kind"] = op.Kind.ToString().ToLowerInvariant(),
            ["method"] = op.Method,
            ["path"] = op.Path,
            ["title"] = op.Summary ?? $"{op.Method} {op.Path}",
            ["description"] = op.Description,
            ["servers"] = new JsonArray(op.Servers.Select(s => (JsonNode?)s).ToArray()),
            ["auth"] = new JsonArray(op.Auth.Select(a => (JsonNode?)new JsonObject { ["kind"] = a.Kind, ["name"] = a.Name, ["in"] = a.In, ["description"] = a.Description }).ToArray()),
            ["params"] = new JsonArray(op.Parameters.Where(p => p.In != "argument").Select(p => (JsonNode?)new JsonObject
            {
                ["name"] = p.Name,
                ["in"] = p.In,
                ["type"] = p.Type,
                ["required"] = p.Required,
                ["value"] = Scalar(p.Example ?? p.Default),
                ["description"] = p.Description,
                ["enum"] = p.Enum is null ? null : new JsonArray(p.Enum.Select(v => (JsonNode?)v).ToArray()),
            }).ToArray()),
            ["labels"] = new JsonObject(PlaygroundLabelKeys.Select(k => KeyValuePair.Create(k, (JsonNode?)l.Text(k)))),
        };

        if (op.Kind == ApiKind.GraphQl)
        {
            data["graphql"] = new JsonObject { ["query"] = op.GraphQlDocument, ["variables"] = ToJson(op.GraphQlVariables) };
        }
        else if (op.Body is { } body)
        {
            data["body"] = new JsonObject
            {
                ["contentType"] = body.ContentType,
                ["required"] = body.Required,
                ["example"] = body.Example is JsonValue text && text.TryGetValue<string>(out var raw) && !body.ContentType.Contains("json") ? raw : ToJson(body.Example),
                ["fields"] = new JsonArray(body.Fields.Select(f => (JsonNode?)new JsonObject
                {
                    ["name"] = f.Name,
                    ["type"] = f.Type,
                    ["required"] = f.Required,
                    ["file"] = f.Type.Contains("binary") || f.Type == "file",
                    ["description"] = f.Description,
                    ["value"] = Scalar(f.Example ?? f.Default),
                }).ToArray()),
            };
        }
        return data.ToJsonString(Embedded);
    }

    private static string ToJson(JsonNode? node) => node is null ? "null" : node.ToJsonString(Pretty);

    private static string Scalar(JsonNode? node) => node switch
    {
        null => "",
        JsonValue value when value.TryGetValue<string>(out var s) => s,
        _ => node.ToJsonString(),
    };

    private static string Slug(string text) => MarkdownService.Slugify(text) is { Length: > 0 } slug ? slug : "field";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
