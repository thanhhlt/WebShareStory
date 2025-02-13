using System.Net.Http.Headers;
using System.Text.Json.Serialization;

public interface IDescriptionService
{
    Task<string> GenerateDescriptionAsync(string content);
}

public class DescriptionService : IDescriptionService
{
    private readonly HttpClient _httpClient;
    private readonly string _huggingFaceApiKey;

    public DescriptionService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _huggingFaceApiKey = apiKey;
    }

    public async Task<string> GenerateDescriptionAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var tokens = content.Split(' ').ToList();

        string truncatedContent = string.Join(" ", tokens.Take(200));

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _huggingFaceApiKey);

        var requestBody = new
        {
            inputs = $"Tóm tắt bài viết: {truncatedContent}",
            parameters = new { max_length = 200 }
        };

        var response = await _httpClient.PostAsJsonAsync("https://api-inference.huggingface.co/models/facebook/bart-large-cnn", requestBody);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<List<HuggingFaceResponse>>();
            return result?.FirstOrDefault()?.SummaryText ?? string.Empty;
        }

        return string.Empty;
    }

    private class HuggingFaceResponse
    {
        [JsonPropertyName("summary_text")]
        public string? SummaryText { get; set; }
    }
}