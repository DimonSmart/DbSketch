using System.Reflection;

namespace DimonSmart.DbSketch.Cli;

public static class DbSketchVersion
{
    public static string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(DbSketchVersion).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = string.IsNullOrWhiteSpace(informationalVersion)
            ? assembly.GetName().Version?.ToString(3)
            : informationalVersion;

        if (string.IsNullOrWhiteSpace(version))
        {
            return "0.0.0";
        }

        version = version.Trim();
        if (version.StartsWith('v') && version.Length > 1 && char.IsDigit(version[1]))
        {
            version = version[1..];
        }

        var metadataStart = version.IndexOf('+', StringComparison.Ordinal);
        return metadataStart < 0 ? version : version[..metadataStart];
    }
}
