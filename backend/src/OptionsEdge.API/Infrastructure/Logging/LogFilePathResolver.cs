namespace OptionsEdge.API.Infrastructure.Logging;

public static class LogFilePathResolver
{
    public static string Resolve(string contentRootPath, string configuredDirectory)
    {
        if (Path.IsPathRooted(configuredDirectory))
            return configuredDirectory;

        return Path.GetFullPath(Path.Combine(contentRootPath, configuredDirectory));
    }
}
