using System.Net.Http.Json;
using System.Text.Json;
using FiscalisationService.Models;

namespace FiscalisationService.Services;

public sealed class FiscalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions;

    public FiscalApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ApiResult> PostReceiptAsync(string url, ReceiptDetails receiptDetails, int timeoutSeconds, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, receiptDetails, _serializerOptions, timeoutCts.Token);
            var raw = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var statusMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}".Trim();
                return new ApiResult(null, false, statusMessage, raw);
            }

            FiscalResponse? payload = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                payload = JsonSerializer.Deserialize<FiscalResponse>(raw, _serializerOptions);
            }

            if (payload is null)
            {
                return new ApiResult(null, false, "Failed to parse API response.", raw);
            }

            return new ApiResult(payload, false, null, raw);
        }
        catch (HttpRequestException ex)
        {
            return new ApiResult(null, false, ex.Message, null);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ApiResult(null, true, $"Timeout after {Math.Max(1, timeoutSeconds)}s", null);
        }
    }
}

public sealed record ApiResult(FiscalResponse? Response, bool TimedOut, string? ErrorMessage, string? RawResponse);
