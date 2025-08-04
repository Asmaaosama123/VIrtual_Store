using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class SegmindService
{
    private readonly HttpClient _httpClient;
    private const string ApiUrl = "https://api.segmind.com/v1/idm-vton";
    private readonly string _apiKey;

    public SegmindService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["Segmind:ApiKey"]; // Store API key in appsettings.json
    }

    public async Task<byte[]> TryOnClothesAsync(string humanImageUrl, string clothingImageUrl, string description)
    {
        var requestData = new
        {
            crop = false,
            seed = 42,
            steps = 30,
            category = "upper_body",
            force_dc = false,
            human_img = humanImageUrl,
            garm_img = clothingImageUrl,
            mask_only = false,
            garment_des = description
        };

        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync(ApiUrl, content);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }

        throw new Exception($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
    }
}
