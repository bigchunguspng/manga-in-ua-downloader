using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Text.RegularExpressions;
using MangaInUaDownloader.Commands;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// miu-dl [-o "Title\Chapter"] URL-chapter
// miu-dl [--translators-list] URL-title // out: ch A - B: tr.1 \n ch C - D: tr.1, tr.2 ...
// miu-dl [--only-translator "name"][--only-translator "name"]
// [--from-chapter int][--to-chapter int][--chapter int]
// [--from-volume int][--to-volume int][--volume int] URL-title

namespace MangaInUaDownloader
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console. InputEncoding = System.Text.Encoding.Unicode;
            
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            /*var d = new Downloader("sadasdasd");
            await d.DownloadChapter(new MangaChapter(), "asdas");

            return await Task.FromResult(1);*/
            //var root = RootCommandBuilder.Build();
            //var handler = new RootCommandHandler(new MangaService());
            //root.SetHandler(chapter => handler.Chapter = chapter);

            var root = RootCommandBuilder.Build();
            var parser = new CommandLineBuilder(root).UseDefaults().Build();
            
            /*
             * .UseHost(_ => Host.CreateDefaultBuilder(args), (builder) =>
                {
                    builder
                        .ConfigureServices(services => { services.TryAddSingleton<MangaService>(); })
                        .ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders())
                        .UseCommandHandler<RootCommand, RootCommandHandler>();
                })
             */

            return await parser.InvokeAsync(args);

            //new InputParser().Run(args);
        }
    }

    /*public class InputParser // miu-dl [-p "Title\Chapter"] URL
    {
        private readonly Regex _url  = new(@"https?:\/\/manga\.in\.ua\/\S+");

        public void Run(string[] input)
        {
            var path = input.Length > 2 && input[0] == "-p" ? input[1] : Environment.CurrentDirectory;

            var url = _url.IsMatch(input[^1]) ? input[^1] : throw new ArgumentException("No URL speified");
            
            new Downloader(path).DownloadChapter(url);
        }
    }*/
}