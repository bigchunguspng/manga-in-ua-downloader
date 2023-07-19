using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Globalization;
using MangaInUaDownloader.Commands;
using MangaInUaDownloader.MangaRequestHandlers;
using MangaInUaDownloader.Services;
using Spectre.Console;

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

            /*await AnsiConsole.Progress()
                .Columns(
                    new TaskNameColumn(),
                    new ProgressBarColumn(),
                    new DownloadedColumn(),
                    new TransferSpeedColumn(),
                    new SpinnerColumn(),
                    new TaskStatusColumn())
                .StartAsync(async ctx =>
                {
                    var name = "Том 2. Розділ 3:";
                    var task1 = ctx.AddTask($"{name}\tGetting urls...");
                    await Task.Delay(2150);
                    task1.Description = $"{name}\tDownloading...";
                    
                    while (!ctx.IsFinished)
                    {
                        task1.Increment(1);
                        await Task.Delay(150);
                    }
                });
            
            return 0;*/

            /*var colors = new List<string>(256);
            foreach (var property in Color.Aqua.GetType().GetProperties())
            {
                if (property.PropertyType == Color.Aqua.GetType())
                {
                    colors.Add($"[{property.Name.ToLower()}]{property.Name.ToLower()}[/]");
                }
            }

            var num = (int)Math.Ceiling(colors.Count / 6D);

            var cols = colors.Chunk(num).ToList();

            var table = new Table().Border(TableBorder.Simple).AddColumn("--").AddColumn("--").AddColumn("--").AddColumn("--").AddColumn("--").AddColumn("--");
            for (int i = 0; i < num; i++)
            {
                table.AddRow(new Markup(cols[0][i]), new Markup(cols[1][i]), new Markup(cols[2][i]), new Markup(cols[3][i]), new Markup(cols[4][i]), new Markup(cols[5].Length > i ? cols[5][i] : ""));
            }
            AnsiConsole.Write(table);*/

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