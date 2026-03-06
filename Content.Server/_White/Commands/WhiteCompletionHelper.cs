using System.Linq;
using Robust.Shared.Console;

namespace Content.Server._White.Commands;

/// <summary>
/// Helper functions for programming console command completions.
/// </summary>
public static class WhiteCompletionHelper
{
    /// <summary>
    /// Returns the enum as completion options.
    /// </summary>
    public static IEnumerable<CompletionOption> Emuns(Type type) => Enum.GetNames(type).Select(name => new CompletionOption(name));
}
