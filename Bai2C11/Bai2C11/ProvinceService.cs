using System.Text.Json;

namespace Bai2C11;

public class Area
{
    public string name { get; set; } = "";
    public int code { get; set; }
}

/// <summary>Gọi API hành chính provinces.open-api.vn (HttpClient dùng chung, timeout 10s).</summary>
public static class ProvinceService
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    static readonly JsonSerializerOptions Opt = new() { PropertyNameCaseInsensitive = true };
    const string Base = "https://provinces.open-api.vn/api/v2/";

    public static async Task<List<Area>> GetProvincesAsync()
    {
        var json = await Http.GetStringAsync(Base);
        return JsonSerializer.Deserialize<List<Area>>(json, Opt) ?? new();
    }

    public static async Task<List<Area>> GetWardsAsync(int provinceCode)
    {
        var json = await Http.GetStringAsync($"{Base}p/{provinceCode}?depth=2");
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("wards", out var arr) &&
            !doc.RootElement.TryGetProperty("districts", out arr))
            return new();
        return JsonSerializer.Deserialize<List<Area>>(arr.GetRawText(), Opt) ?? new();
    }
}
