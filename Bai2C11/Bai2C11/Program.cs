using System.Globalization;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Bai2C11;

static class Program
{
    public static AppSettings Settings { get; private set; } = new();

    [STAThread]
    static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture = new CultureInfo("vi-VN");

        Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
            .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Phải gọi trước khi tạo bất kỳ Form nào
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => XuLyLoi(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => XuLyLoi(e.ExceptionObject as Exception);

        ApplicationConfiguration.Initialize();
        try
        {
#if DEBUG
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
#else
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
#endif
            var cfg = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true).Build();
            Settings = cfg.Get<AppSettings>() ?? new();
            Application.Run(new FormKhachThue());
        }
        catch (Exception ex) { XuLyLoi(ex); }
        finally { Log.CloseAndFlush(); }
    }

    static void XuLyLoi(Exception? ex)
    {
        Log.Error(ex, "Lỗi chưa được bắt");   // có đầy đủ stack trace
        MessageBox.Show("Đã xảy ra lỗi, thông tin đã được ghi lại.\nỨng dụng vẫn tiếp tục chạy.",
            "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
