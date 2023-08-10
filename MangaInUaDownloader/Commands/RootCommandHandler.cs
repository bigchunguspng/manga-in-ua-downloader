using System.CommandLine.Invocation;
using MangaInUaDownloader.MangaRequestHandlers;
using Spectre.Console;

namespace MangaInUaDownloader.Commands
{
    public class RootCommandHandler : ICommandHandler
    {
        private List<MangaRequestHandler>? MangaHandlers;
        
        private string? URL;

        public RootCommandHandler WithTheseSubhandlers(List<MangaRequestHandler> list)
        {
            MangaHandlers = list;

            return this;
        }

        public int Invoke(InvocationContext context) => InvokeAsync(context).Result;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            URL = context.ParseResult.GetValueForArgument(RootCommandBuilder.URLArg).ToString();

            if (MangaHandlers is null)
            {
                AnsiConsole.MarkupLine($"\nAdd at least one [fuchsia]{nameof(MangaRequestHandler)}[/] implementation to your [fuchsia]{nameof(RootCommandHandler)}[/] object.\n");
                
                return -1;
            }
            
            foreach (var handler in MangaHandlers)
            {
                if (handler.CanHandleThis(URL))
                {
                    return await handler.InvokeAsync(context);
                }
            }
            AnsiConsole.MarkupLine("\nПеревірте [fuchsia]URL[/]. Поточна версія програми підтримує лише наступні ресурси:\n");
            foreach (var handler in MangaHandlers) AnsiConsole.MarkupLine($"[deeppink3][[{handler.MANGA_WEBSITE}]][/]");
            AnsiConsole.WriteLine();

            return -1;
        }
    }
}