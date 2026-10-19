using System.Globalization;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Bai3C11;

static class Program
{
    public static AppSettings Settings { get; private set; } = new();
    public static UserSettings User { get; private set; } = new();

    [STAThread]
    static void Main()
    {
        Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
            .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Loi(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Loi(e.ExceptionObject as Exception);
        ApplicationConfiguration.Initialize();
        try
        {
#if DEBUG
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
#else
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
#endif
            Settings = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true).Build().Get<AppSettings>() ?? new();
            User = UserSettings.Load();               // ngôn ngữ đã lưu lần trước
            L.Lang = User.Language == "en" ? "en" : "vi";
            Application.Run(new FormChuyenDi());
        }
        catch (Exception ex) { Loi(ex); }
        finally { Log.CloseAndFlush(); }
    }

    static void Loi(Exception? ex)
    {
        Log.Error(ex, "Lỗi chưa được bắt");
        MessageBox.Show("Đã xảy ra lỗi, thông tin đã được ghi lại.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
