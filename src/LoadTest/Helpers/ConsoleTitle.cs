using System.Reflection;

namespace LoadTest.Helpers;

/// <summary>
/// Shows the app name and running command in the terminal title.
/// </summary>
public static class ConsoleTitle
{
    private static string? _original;

    public static void Set(string? command)
    {
        var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "LoadTest";

        try
        {
            if (OperatingSystem.IsWindows())
            {
                _original ??= Console.Title;
            }

            Console.Title = string.IsNullOrWhiteSpace(command) ? appName : $"{appName} - {command}";
        }
        catch (Exception ex) when (ex is IOException or PlatformNotSupportedException or UnauthorizedAccessException)
        {
            // A title is cosmetic. Ignore consoles that don't support it.
        }
    }

    /// <summary>
    /// Puts back the title that was in place before <see cref="Set"/>. Only possible on Windows.
    /// </summary>
    public static void Restore()
    {
        if (_original is null)
        {
            return;
        }

        try
        {
            Console.Title = _original;
        }
        catch (Exception ex) when (ex is IOException or PlatformNotSupportedException or UnauthorizedAccessException)
        {
            // Cosmetic only.
        }
    }
}
