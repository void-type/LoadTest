using System.CommandLine;
using System.CommandLine.Invocation;

namespace LoadTest.Commands;

public static class CommandExtensions
{
    public static T GetValueForOptionEnsureNotNull<T>(this InvocationContext context, Option<T> option)
    {
        return context.ParseResult.GetValueForOption(option) ?? throw new ArgumentNullException(option.Name);
    }
}
