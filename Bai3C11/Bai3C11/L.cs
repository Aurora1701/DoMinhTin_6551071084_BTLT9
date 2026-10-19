using System.Globalization;
using System.Text.Json;
using Serilog;

namespace Bai3C11;

/// <summary>Từ điển đa ngôn ngữ vi/en + CultureInfo tương ứng.</summary>
public static class L
{
    public static string Lang = "vi";
    public static CultureInfo Culture => new(Lang == "vi" ? "vi-VN" : "en-US");
    public static string T(string k) => D.TryGetValue(k, out var v) ? (Lang == "vi" ? v.vi : v.en) : k;
    public static string T(string k, params object[] a) => string.Format(Culture, T(k), a);

    static readonly Dictionary<string, (string vi, string en)> D = new()
    {
        ["title"] = ("Kế hoạch dã ngoại", "Picnic Trip Planner"),
        ["lang"] = ("Ngôn ngữ", "Language"),
        ["info"] = ("Thông tin chuyến đi", "Trip information"),
        ["ma"] = ("Mã", "ID"),
        ["tenChuyen"] = ("Tên chuyến", "Trip name"),
        ["diaDiem"] = ("Địa điểm", "Destination"),
        ["ngayDi"] = ("Ngày đi", "Date"),
        ["soNguoi"] = ("Số người", "People"),
        ["nganSach"] = ("Ngân sách", "Budget"),
        ["phuongTien"] = ("Phương tiện", "Transport"),
        ["daHT"] = ("Đã hoàn thành", "Completed"),
        ["hoanThanh"] = ("Hoàn thành", "Done"),
        ["them"] = ("Thêm", "Add"), ["sua"] = ("Sửa", "Edit"), ["xoa"] = ("Xóa", "Delete"), ["lamMoi"] = ("Làm mới", "Reset"),
        ["timPH"] = ("Tìm tên chuyến / địa điểm...", "Search trip / place..."),
        ["chiChuaHT"] = ("Chỉ hiện chuyến chưa hoàn thành", "Only show unfinished trips"),
        ["locNgay"] = ("Lọc theo ngày", "Filter by date"),
        ["tu"] = ("Từ", "From"), ["den"] = ("Đến", "To"), ["tim"] = ("Tìm", "Search"),
        ["xemTT"] = ("Xem thời tiết", "Check weather"),
        ["dubao"] = ("Dự báo thời tiết ngày {0} - {1}", "Weather forecast for {0} - {1}"),
        ["nhiet"] = ("Nhiệt độ: {0}°C - {1}°C", "Temperature: {0}°C - {1}°C"),
        ["mua"] = ("Khả năng mưa: {0}%", "Chance of rain: {0}%"),
        ["khuyen"] = ("Nên mang áo mưa hoặc cân nhắc dời lịch", "Bring a raincoat or consider rescheduling"),
        ["noData"] = ("Chưa có dữ liệu dự báo", "No forecast data yet"),
        ["notFound"] = ("Không tìm thấy địa danh. Thử tên không dấu, ví dụ \"Da Lat\".", "Place not found. Try another spelling, e.g. \"Da Lat\"."),
        ["netErr"] = ("Không kết nối được dịch vụ thời tiết. Vui lòng kiểm tra mạng.", "Cannot reach the weather service. Please check your network."),
        ["timeout"] = ("Dịch vụ thời tiết phản hồi quá chậm. Thử lại sau.", "Weather service timed out. Please try again."),
        ["loading"] = ("Đang tải dự báo...", "Loading forecast..."),
        ["w0"] = ("Trời quang", "Clear sky"), ["w1"] = ("Có mây", "Cloudy"), ["w2"] = ("Sương mù", "Fog"),
        ["w3"] = ("Mưa", "Rain"), ["w4"] = ("Dông", "Thunderstorm"), ["w5"] = ("Tuyết", "Snow"), ["wX"] = ("Không xác định", "Unknown"),
        ["tp_Xe máy"] = ("Xe máy", "Motorbike"), ["tp_Ô tô"] = ("Ô tô", "Car"),
        ["tp_Xe khách"] = ("Xe khách", "Coach"), ["tp_Tàu hỏa"] = ("Tàu hỏa", "Train"),
        ["errTitle"] = ("Dữ liệu chưa hợp lệ", "Invalid data"),
        ["errTen"] = ("Tên chuyến và địa điểm không được để trống.", "Trip name and destination are required."),
        ["errNgay"] = ("Ngày đi không được nhỏ hơn hôm nay.", "Date cannot be before today."),
        ["errNguoi"] = ("Số người phải ≥ 1.", "People must be ≥ 1."),
        ["errNS"] = ("Ngân sách phải ≥ 0.", "Budget must be ≥ 0."),
        ["chon"] = ("Hãy bấm chọn một chuyến trong danh sách.", "Please select a trip in the list."),
        ["confirmDel"] = ("Bạn có chắc muốn xóa chuyến đi này?", "Delete this trip?"),
        ["confirm"] = ("Xác nhận", "Confirm"),
        ["errDb"] = ("Không truy cập được cơ sở dữ liệu. Vui lòng kiểm tra kết nối.", "Cannot access the database. Please check the connection."),
        ["err"] = ("Lỗi", "Error"),
        ["needPlace"] = ("Hãy nhập địa điểm trước.", "Please enter a destination first."),
    };
}

public class UserSettings
{
    public string Language { get; set; } = "vi";
    static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                           "Bai3C11", "usersettings.json");
    public static UserSettings Load()
    {
        try { if (File.Exists(FilePath)) return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception ex) { Log.Warning(ex, "Không đọc được usersettings.json"); }
        return new();
    }
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { Log.Warning(ex, "Không lưu được usersettings.json"); }
    }
}
