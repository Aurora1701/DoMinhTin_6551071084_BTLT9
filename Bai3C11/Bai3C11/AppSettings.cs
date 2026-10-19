namespace Bai3C11;

public class AppSettings
{
    public ConnectionStringsSection ConnectionStrings { get; set; } = new();
}

public class ConnectionStringsSection
{
    public string Default { get; set; } = "";
}
