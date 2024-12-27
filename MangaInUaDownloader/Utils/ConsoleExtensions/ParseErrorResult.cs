using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using Spectre.Console;

namespace MangaInUaDownloader.Utils.ConsoleExtensions;

public class ParseErrorResult : IInvocationResult
{
    private readonly HelpBuilder _help;

    public ParseErrorResult(HelpBuilder help) => _help = help;

    /// <inheritdoc />
    public void Apply(InvocationContext context)
    {
        if (context.ParseResult.Tokens.Count == 0) // 0 args >> show help
        {
            var helpContext = new HelpContext
            (
                _help,
                context.ParseResult.CommandResult.Command,
                context.Console.Out.CreateTextWriter(),
                context.ParseResult
            );

            _help.Write(helpContext);

            context.ExitCode = 0;
        }
        else // tell about error(s)
        {
            foreach (var error in context.ParseResult.Errors)
            {
                AnsiConsole.MarkupLine($"[red]{error.Message.EscapeMarkup()}[/]");
            }

            AnsiConsole.MarkupLine("\nВикористайте опцію [yellow]--help[/] для перегляду довідки.\n");

            context.ExitCode = 1;
        }
    }
}