using System.Globalization;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Bai1C11;

static class Program
{
    public static AppSettings Settings { get; private set; } = new();

    [STAThread]
    static void Main()
    {
        var vi = new CultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture = vi;
        ApplicationConfiguration.Initialize();

        // Log theo ngày: Logs\log-yyyyMMdd.txt cạnh file .exe
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"),
                          rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
#if DEBUG
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
#else
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
#endif
            var cfg = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true)   // ghi đè khi lập trình
                .Build();
            Settings = cfg.Get<AppSettings>() ?? new AppSettings();
            Log.Information("Khởi động ứng dụng (env={Env})", env);
            Application.Run(new FormChiTieu());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Ứng dụng gặp lỗi nghiêm trọng khi khởi động");
            MessageBox.Show("Không thể khởi động ứng dụng. Vui lòng kiểm tra file appsettings.json.",
                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { Log.CloseAndFlush(); }
    }
}
