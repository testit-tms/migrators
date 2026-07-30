using System.Reflection;

namespace Models;

public static class AppVersion
{
    public static string Current =>
        (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version?.ToString()
        ?? "unknown";
}
