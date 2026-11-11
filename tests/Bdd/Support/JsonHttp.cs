using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace E2e.Bdd.Support;

public sealed class JsonHttp
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public void SetBearer(string? token)
    {
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<JsonElement> SendAsync(
        HttpMethod method,
        string url,
        object? body = null,
        IEnumerable<KeyValuePair<string, string>>? headers = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        using var response = await _http.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"{method} {url} → {(int)response.StatusCode}: {raw}");
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return default;
        }

        return JsonSerializer.Deserialize<JsonElement>(raw, JsonOptions);
    }

    public Task<JsonElement> GetAsync(string url) => SendAsync(HttpMethod.Get, url);

    public Task<JsonElement> PostAsync(string url, object? body = null) =>
        SendAsync(HttpMethod.Post, url, body);

    public async Task ResetWireMockRequestsAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceUrls.WireMock}/__admin/requests");
        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> WaitForWireMockOrderIdAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var journal = await GetAsync($"{ServiceUrls.WireMock}/__admin/requests");
            if (journal.TryGetProperty("requests", out var requests)
                && requests.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in requests.EnumerateArray())
                {
                    if (!entry.TryGetProperty("request", out var req))
                    {
                        continue;
                    }

                    var method = req.TryGetProperty("method", out var m) ? m.GetString() : null;
                    var url = req.TryGetProperty("url", out var u) ? u.GetString() : null;
                    if (!string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
                        || url is null
                        || !url.Contains("/v1/orders", StringComparison.Ordinal)
                        || url.Contains("/cancel", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (entry.TryGetProperty("response", out var resp)
                        && resp.TryGetProperty("body", out var bodyEl))
                    {
                        var bodyText = bodyEl.GetString();
                        if (!string.IsNullOrWhiteSpace(bodyText))
                        {
                            using var doc = JsonDocument.Parse(bodyText);
                            if (doc.RootElement.TryGetProperty("id", out var id)
                                && id.GetString() is { Length: > 0 } orderId)
                            {
                                return orderId;
                            }
                        }
                    }
                }
            }

            await Task.Delay(500);
        }

        throw new TimeoutException("WireMock não registrou POST /v1/orders a tempo.");
    }
}
