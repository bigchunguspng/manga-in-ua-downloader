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

            var help = new CustomHelpBuilder(Localization.Instance, Console.WindowWidth);
            help.HideDefaultValue(RootCommandBuilder.ChapterOption);
            help.HideDefaultValue(RootCommandBuilder.FromChapterOption);
            help.HideDefaultValue(RootCommandBuilder.ToChapterOption);
            help.HideDefaultValue(RootCommandBuilder.VolumeOption);
            help.HideDefaultValue(RootCommandBuilder.FromVolumeOption);
            help.HideDefaultValue(RootCommandBuilder.ToVolumeOption);

            var requestHandlers = new List<MangaRequestHandler> { new MangaInUaHandler(new MangaInUaService()) };
            var handler = new RootCommandHandler().WithTheseSubhandlers(requestHandlers);
            var command = RootCommandBuilder.Build(handler);
            var parser  = new CommandLineBuilder(command)
                .UseLocalizationResources(Localization.Instance)
                .UseVersionOption("-!", "--version")
                .UseHelp         ("-?", "--help")
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseParseErrorReporting(help)
                .UseExceptionHandler()
                .CancelOnProcessTermination()
                .UseHelpBuilder(_ => help).Build();

            return await parser.InvokeAsync(args);
        }
    }
}