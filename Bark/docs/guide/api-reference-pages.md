---
title: API reference pages
description: Endpoint and object pages generated from OpenAPI, GraphQL and OData specs, with a browser-side playground
---

# API reference pages

An API reference page is a Markdown page with an `api` front matter key. The key names a spec file and one operation or object in that spec. Bark renders the endpoint, authorizations, parameters, body, responses, request samples, response examples and a playground.

```yaml
---
api: openapi.yaml POST /tasks/{taskId}
---

Markdown below the front matter renders under the operation description.
```

::: tip Example
We've set-up an example! [Example API](/examples/api) is an API section generated from an OpenAPI specification. **Try it** sends the request to [httpbin.org](https://httpbin.org) and displays the response.
:::

## Front matter

| Key | Value | Description |
|---|---|---|
| `api` | `<file> <selector> <name>` | Spec file and operation or object. Required. |
| `title` | `string` | Page title. Default: the operation summary, the object name, or `<METHOD> <path>`. |
| `apiSpec` | `<file>` | Generates an [API section](#api-sections) from the spec file. Set on a folder index page. `server` and `auth` apply to every generated page. |
| `server` | URL | Base URL for requests and samples. Overrides the spec servers. Required for GraphQL and OData specs. |
| `auth` | `bearer`, `basic` or `apiKey <in> <name>` | Authentication scheme. Overrides the spec security. `<in>` is `header` or `query`. |

The spec file path is relative to the page. A leading `/` resolves the path from the docs root. The file must be inside the docs folder. URLs are rejected.

## Spec formats

| Format | File extensions | Operation selector | Object selector |
|---|---|---|---|
| OpenAPI 3.0, 3.1, 3.2 and Swagger 2.0 (YAML or JSON) | `.yaml`, `.yml`, `.json` | `<METHOD> <path>`, `webhook <name>` | `schema <name>` |
| GraphQL SDL | `.graphql`, `.graphqls`, `.gql` | `query <field>`, `mutation <field>`, `subscription <field>` | `schema <type>` |
| OData CSDL (`$metadata`), versions 2 to 4 | `.xml`, `.edmx`, `.csdl` | `<METHOD> <EntitySet>`, `<METHOD> <EntitySet>(key)`, `<METHOD> <Import>` | `schema <type>` |

```yaml
api: openapi.yaml QUERY /tasks          # OpenAPI 3.2 QUERY method
api: openapi.yaml PURGE /cache          # OpenAPI 3.2 additionalOperations
api: openapi.yaml webhook taskProcessed # webhook payload, no playground
api: openapi.yaml schema FileData       # object page
api: schema.graphql query country
api: metadata.xml GET People(key)       # renders as /People({UserName})
api: metadata.xml POST ResetDataSource  # action import
```

### OpenAPI

- Methods: `GET`, `PUT`, `POST`, `DELETE`, `OPTIONS`, `HEAD`, `PATCH`, `TRACE`, `QUERY`, and custom methods defined in `additionalOperations`.
- Parameter locations: `path`, `query`, `querystring`, `header`, `cookie`. Parameters defined with `content` read the schema of the first media type.
- `$ref` resolves within the same document. `allOf` merges into one attribute list. `oneOf` and `anyOf` list the variant types and expand the first variant.
- `itemSchema` describes each item of a sequential media type (JSON Lines, server-sent events).
- Request bodies exclude `readOnly` properties. Responses exclude `writeOnly` properties.
- Examples are read from `example`, `examples`, `default` and `enum`. Missing examples are generated from the schema.

### GraphQL

- Arguments render as the `Arguments` section and as request variables.
- The request sample selects scalar and enum fields to a depth of 2. Fields with required arguments are excluded from the selection.
- `subscription` operations are documented without a playground.

### OData

| Selector | Method | Path | Query options |
|---|---|---|---|
| `GET People` | `GET` | `/People` | `$filter`, `$select`, `$expand`, `$orderby`, `$top`, `$skip`, `$count`, `$search` |
| `POST People` | `POST` | `/People` | none |
| `GET People(key)` | `GET` | `/People({UserName})` | `$select`, `$expand` |
| `PATCH People(key)` | `PATCH` | `/People({UserName})` | none |
| `DELETE People(key)` | `DELETE` | `/People({UserName})` | none |
| `GET <FunctionImport>` | `GET` | `/<Function>(p1={p1})` | none |
| `POST <ActionImport>` | `POST` | `/<Action>` | none |

Properties annotated `Core.Computed` or `Core.Immutable` are read-only. Descriptions are read from `Core.Description` annotations.

## API sections

An API section is a folder generated from one spec file. The folder index page sets `apiSpec`:

```yaml
---
title: Example API
apiSpec: openapi.yaml
---
```

Bark generates one page per operation and object, and a sidebar for the folder. The index page is the first sidebar entry.

| Format | Sidebar groups | Page slug |
|---|---|---|
| OpenAPI | Tags in spec order, then `Endpoints` (untagged), `Webhooks`, `Objects` | `operationId`, else `summary` |
| GraphQL | `Queries`, `Mutations`, `Subscriptions`, `Types` | Field or type name |
| OData | One group per entity set, then `Operations`, `Types` | Method and entity set |

- A Markdown file with the same slug replaces the generated page. Example: `update-task.md` with `api: openapi.yaml POST /tasks/{id}` adds Markdown to that operation.
- A `sidebar` key in `config.json` for the same folder replaces the generated sidebar.
- Generated pages link **Edit this page** to the spec file.
- Group names are translatable in the locale files (`apiGroupEndpoints`, `apiGroupObjects` and related keys).

## Navigation

Single API pages use the standard sidebar configuration. A `sidebar` key with a path prefix assigns a separate menu to every page under that folder. Nested groups define namespaces.

```json
{
  "sidebar": {
    "/api/": [
      {
        "title": "Tasks",
        "items": [
          { "title": "Get task", "path": "api/get-task" },
          { "title": "Update task", "path": "api/update-task" }
        ]
      }
    ]
  }
}
```

Sidebar entries for API operation pages display the HTTP method. See [Sidebar](/reference/default-theme-sidebar) for prefix matching and collapsible groups.

## Page layout

| Region | Content |
|---|---|
| Endpoint bar | Method, server URL, path and **Try it** button. |
| Main column | Description, page Markdown, then `Authorizations`, parameter sections, `Body` and `Response`. |
| Side column | Request samples (cURL, Python, JavaScript, PHP, Go, Java) and response examples per status code. |
| Sidebar | Entries for API pages display the HTTP method (`HOOK` for webhooks). |

Nested attributes are collapsed under **Show child attributes**. API pages have no table of contents. Below `1100px`, the side column renders below the main column.

## Playground

**Try it** opens the playground. The left column contains inputs for authorizations, parameters and the body. The right column displays the example responses, then the live response: status, duration, size, body and headers.

- Requests are sent from the reader's browser to the API. Bark does not send or forward requests.
- Browser cookies are not sent.
- Credentials and playground input are kept in memory only. Nothing is written to browser storage. Reloading or leaving the page clears them.
- Requests are limited to the spec servers or the `server` URL.
- The API must allow cross-origin requests (CORS) from the docs site, including every documented method, such as `QUERY`.
- Cookie parameters and cookie API keys are not sent. Browsers block the `Cookie` header.
- JSON and text bodies use one text field. Form bodies (`multipart/form-data`, `application/x-www-form-urlencoded`) use one input per attribute and a file picker for `binary` attributes.

## Localization

Section headings, badges and playground labels are translatable in the locale files. The keys start with `api`, for example `apiTryIt` and `apiSend`.

## Errors

If the specification file, the selector or the content is invalid: the page displays the error above the page Markdown. The error is also written to the log.
