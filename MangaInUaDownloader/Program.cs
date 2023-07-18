using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Globalization;
using MangaInUaDownloader.Commands;
using MangaInUaDownloader.MangaRequestHandlers;
using MangaInUaDownloader.Services;

// miu-dl [-o "Title\Chapter"] URL-chapter
// miu-dl [--translators-list] URL-title // out: ch A - B: tr.1 \n ch C - D: tr.1, tr.2 ...
// miu-dl [--only-translator "name"][--only-translator "name"]
// [--from-chapter int][--to-chapter int][--chapter int]
// [--from-volume int][--to-volume int][--volume int] URL-title

// todo: url validation, 0 collection messages etc.

namespace MangaInUaDownloader
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console. InputEncoding = System.Text.Encoding.Unicode;
            
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            //Console.WriteLine(new MangaInUaService().GetMangaTitle("https://manga.in.ua/chapters/42053-stvorenij-u-bezodni-tom-1-rozdil-3.html"));

            //return 0;

            var list = new List<MangaRequestHandler>() { new MangaInUaHandler(new MangaInUaService()) };
            var handler = new RootCommandHandler().WithTheseSubhandlers(list);
            var command = RootCommandBuilder.Build(handler);
            var parser  = new CommandLineBuilder(command).UseDefaults().Build();

            return await parser.InvokeAsync(args);
            
            /*
             * .UseHost(_ => Host.CreateDefaultBuilder(args), (builder) =>
                {
                    builder
                        .ConfigureServices(services => { services.TryAddSingleton<MangaService>(); })
                        .ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders())
                        .UseCommandHandler<RootCommand, RootCommandHandler>();
                })
             */
        }
    }
}