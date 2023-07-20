using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Globalization;
using MangaInUaDownloader.Commands;
using MangaInUaDownloader.MangaRequestHandlers;
using MangaInUaDownloader.Services;

namespace MangaInUaDownloader
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console. InputEncoding = System.Text.Encoding.Unicode;
            
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            var handlers = new List<MangaRequestHandler>() { new MangaInUaHandler(new MangaInUaService()) };
            var command = RootCommandBuilder.Build(new RootCommandHandler().WithTheseSubhandlers(handlers));
            var parser  = new CommandLineBuilder(command).UseDefaults().Build();

            return await parser.InvokeAsync(args);
            
            /*
             * .UseHost(_ => Host.CreateDefaultBuilder(args), (builder) =>
                {
                    builder
                        .ConfigureServices(services => { services.TryAddSingleton<MangaService>(); })
                        .ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders())
                        .UseCommandHandler<RootCommand, RootCommandHandler>();
                }) // bruh i definetly gonna need this at some point
             */
        }
    }
}