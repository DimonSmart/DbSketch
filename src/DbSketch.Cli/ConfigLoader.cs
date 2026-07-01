using System.Text.RegularExpressions;
using DimonSmart.DbSketch.Core.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DimonSmart.DbSketch.Cli;

public static class ConfigLoader
{
    private static readonly Regex EnvironmentVariablePattern = new(@"\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)(:-(?<fallback>[^}]*))?\}", RegexOptions.CultureInvariant);

    public static DbSketchConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new CliException($"Config file '{path}' was not found.");
        }

        try
        {
            var yaml = ExpandEnvironmentVariables(File.ReadAllText(path));
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<DbSketchConfig>(yaml) ?? new DbSketchConfig();
        }
        catch (CliException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CliException($"Invalid YAML config '{path}': {ex.Message}");
        }
    }

    public static string ExpandEnvironmentVariables(string value) =>
        EnvironmentVariablePattern.Replace(value, match =>
        {
            var name = match.Groups["name"].Value;
            var environmentValue = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(environmentValue))
            {
                return environmentValue;
            }

            var fallback = match.Groups["fallback"];
            if (fallback.Success)
            {
                return fallback.Value;
            }

            throw new CliException($"Environment variable '{name}' is not defined.");
        });
}
