using System.Globalization;
using System.Text.Json;

namespace Bai3C11;

public enum WStatus { Ok, PlaceNotFound, NoForecast }
public record WResult(WStatus Status, string Place = "", int Code = 0, double Min = 0, double Max = 0, int Rain = 0);

/// <summary>Open-Meteo: geocoding -> forecast. HttpClient dùng chung, Timeout 8 giây.</summary>
public static class WeatherService
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static async Task<WResult> GetAsync(string place, DateOnly date)
    {
        int ngay = date.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
        if (ngay < 0 || ngay >= 16) return new(WStatus.NoForecast);   // chỉ dự báo 16 ngày

        // Bước 1: tên địa danh -> toạ độ
        var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(place)}&count=1&language={L.Lang}";
        using var geo = JsonDocument.Parse(await Http.GetStringAsync(geoUrl));
        if (!geo.RootElement.TryGetProperty("results", out var rs) || rs.GetArrayLength() == 0)
            return new(WStatus.PlaceNotFound);
        var r0 = rs[0];
        double lat = r0.GetProperty("latitude").GetDouble(), lon = r0.GetProperty("longitude").GetDouble();
        var tenChuan = r0.TryGetProperty("name", out var n) ? n.GetString() ?? place : place;

        // Bước 2: dự báo theo ngày
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(Inv)}&longitude={lon.ToString(Inv)}" +
                  "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max&timezone=auto&forecast_days=16";
        using var fc = JsonDocument.Parse(await Http.GetStringAsync(url));
        var daily = fc.RootElement.GetProperty("daily");
        var times = daily.GetProperty("time").EnumerateArray().Select(x => x.GetString()).ToList();
        int i = times.IndexOf(date.ToString("yyyy-MM-dd", Inv));
        if (i < 0) return new(WStatus.NoForecast);

        double Num(string k) { var e = daily.GetProperty(k)[i]; return e.ValueKind == JsonValueKind.Null ? 0 : e.GetDouble(); }
        return new(WStatus.Ok, tenChuan, (int)Num("weather_code"), Num("temperature_2m_min"), Num("temperature_2m_max"), (int)Num("precipitation_probability_max"));
    }
}
