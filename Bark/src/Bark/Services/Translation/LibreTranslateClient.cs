using System.Text.Json.Serialization;

namespace Bark.Services.Translation;

public sealed record LibreTranslateRequest(
    [property: JsonPropertyName("q")] string Q,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("api_key")] string? ApiKey);

public sealed record LibreTranslateResponse(
    [property: JsonPropertyName("translatedText")] string? TranslatedText);

public sealed class LibreTranslateClient(HttpClient http, string endpoint, string source, string target, string? apiKey)
{
    public async Task<string> TranslateAsync(string text, CancellationToken cancellationToken)
    {
        var request = new LibreTranslateRequest(text, source, target, "text", apiKey);
        using var response = await http.PostAsJsonAsync($"{endpoint.TrimEnd('/')}/translate", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>(cancellationToken);
        return payload?.TranslatedText ?? text;
    }
}
