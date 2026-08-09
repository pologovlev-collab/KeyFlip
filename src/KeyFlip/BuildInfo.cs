using System.Reflection;

namespace KeyFlip;

internal static class BuildInfo
{
    private static readonly string InformationalVersion = typeof(BuildInfo).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "0.0.0-unknown";

    internal static string Version { get; } = InformationalVersion.Split('+', 2)[0];
    internal static string Commit { get; } = GetCommit(InformationalVersion);
    internal static string ProcessPath { get; } = Path.GetFullPath(
        Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "KeyFlip.exe"));
    internal static string StartupMetadata => $"version={Version} commit={Commit} path={ProcessPath}";
    internal static string DisplayVersion => $"KeyFlip {Version}";

    private static string GetCommit(string informationalVersion)
    {
        var separator = informationalVersion.IndexOf('+');
        if (separator < 0 || separator + 1 >= informationalVersion.Length) return "unknown";

        var revision = informationalVersion[(separator + 1)..];
        return revision.Length > 7 ? revision[..7] : revision;
    }
}
