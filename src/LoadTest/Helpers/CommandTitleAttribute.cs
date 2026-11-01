using Cocona.Filters;

namespace LoadTest.Helpers;

/// <summary>
/// Shows the app name and running command in the terminal title while a command runs.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandTitleAttribute : CommandFilterAttribute
{
    public override async ValueTask<int> OnCommandExecutionAsync(CoconaCommandExecutingContext ctx, CommandExecutionDelegate next)
    {
        ConsoleTitle.Set(ctx.Command.Name);

        try
        {
            return await next(ctx);
        }
        finally
        {
            ConsoleTitle.Restore();
        }
    }
}
