using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Globalization;
using MangaInUaDownloader.Commands;
using MangaInUaDownloader.MangaRequestHandlers;
using MangaInUaDownloader.Services;
using MangaInUaDownloader.Utils.ConsoleExtensions;

namespace MangaInUaDownloader
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            var requestHandlers = new List<MangaRequestHandler> { new MangaInUaHandler(new MangaInUaService()) };
            var handler = new RootCommandHandler().WithTheseSubhandlers(requestHandlers);
            var command = RootCommandBuilder.Build(handler);
            var parser  = new CommandLineBuilder(command)
                .UseLocalizationResources(Localization.Instance)
                .UseVersion("-!", "--version")
                .UseHelp   ("-?", "--help")
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .CancelOnProcessTermination()
                .UseHelpBuilder(_ => CliConfigDumpster.HelpBuilder).Build();

            return await parser.InvokeAsync(args);
        }
    }
}