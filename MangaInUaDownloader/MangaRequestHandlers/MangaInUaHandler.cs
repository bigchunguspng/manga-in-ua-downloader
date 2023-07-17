using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using MangaInUaDownloader.Commands;
using MangaInUaDownloader.Model;
using MangaInUaDownloader.Services;
using MangaInUaDownloader.Utils;
using Spectre.Console;
using Range = MangaInUaDownloader.Utils.Range;

namespace MangaInUaDownloader.MangaRequestHandlers
{
    public class MangaInUaHandler : MangaRequestHandler
    {
        private readonly Regex _url = new(@"^https?:\/\/manga\.in\.ua\/(?:(?:mangas)|(?:chapters))\/\S+");
        
        private readonly MangaService _mangaService;
        
        private float Chapter, FromChapter, ToChapter;
        private int Volume, FromVolume, ToVolume;
        private bool Directory, Chapterize;
        private string? Translator;
        private bool DownloadOtherTranslators;
        private bool ListChapters, ListSelected;
        private string? URL;

        public MangaInUaHandler(MangaService mangaService)
        {
            _mangaService = mangaService;
        }

        public override bool CanHandleThis(string url) => _url.IsMatch(url);

        public override async Task<int> InvokeAsync(InvocationContext context)
        {
            URL = context.ParseResult.GetValueForArgument(RootCommandBuilder.URLArg).ToString();
            
            Chapter = context.ParseResult.GetValueForOption(RootCommandBuilder.ChapterOption);
            FromChapter = context.ParseResult.GetValueForOption(RootCommandBuilder.FromChapterOption);
            ToChapter = context.ParseResult.GetValueForOption(RootCommandBuilder.ToChapterOption);
            Volume = context.ParseResult.GetValueForOption(RootCommandBuilder.VolumeOption);
            FromVolume = context.ParseResult.GetValueForOption(RootCommandBuilder.FromVolumeOption);
            ToVolume = context.ParseResult.GetValueForOption(RootCommandBuilder.ToVolumeOption);

            Directory = context.ParseResult.GetValueForOption(RootCommandBuilder.DirectoryOption);
            Chapterize = context.ParseResult.GetValueForOption(RootCommandBuilder.ChapterizeOption);
            ListChapters = context.ParseResult.GetValueForOption(RootCommandBuilder.ListChaptersOption);
            ListSelected = context.ParseResult.GetValueForOption(RootCommandBuilder.ListSelectedOption);

            var ot = context.ParseResult.GetValueForOption(RootCommandBuilder.OnlyTranslatorOption);
            var pt = context.ParseResult.GetValueForOption(RootCommandBuilder.PreferTranslatorOption);
            Translator = ot ?? pt;
            DownloadOtherTranslators = ot is null;
            
            if    (_mangaService.IsChapterURL(URL))
            {
                var downloader = new ImageDownloader(Chapterize, Directory, _mangaService);
                await downloader.DownloadChapter(new MangaChapter { Volume = int.MinValue, URL = URL}, "");
            }
            else if (_mangaService.IsMangaURL(URL))
            {
                var c = Chapter < 0 ? new RangeF(FromChapter, ToChapter) : new RangeF(Chapter, Chapter);
                var v = Volume  < 0 ? new Range (FromVolume,  ToVolume ) : new Range (Volume,  Volume );

                var options = new MangaDownloadOptions(c, v, Translator, DownloadOtherTranslators);
                var chapters = (await _mangaService.GetChapters(URL, options)).ToList();
                foreach (var chapter in chapters)
                {
                    Console.WriteLine($"Vol. {chapter.Volume} Ch. {chapter.Chapter} {chapter.Title} (by {chapter.Translator}{(chapter.IsAlternative ? " (ALT)" : "")})");
                }

                if (!ListSelected)
                {
                    var downloader = new ImageDownloader(Chapterize);
                    await downloader.DownloadChapters(chapters);
                }
            }
            else if (ListChapters)
            {
                var chapters = await _mangaService.GetChaptersGrouped(URL);
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.White)
                    .AddColumn(new TableColumn("VOL").RightAligned())
                    .AddColumn(new TableColumn("CH").RightAligned())
                    .AddColumn(new TableColumn("TITLE"))
                    .AddColumn(new TableColumn("TRANSLATED BY"))
                    .AddColumn(new TableColumn("ALT"));
                foreach (var title in chapters)
                {
                    var c = title.Value.First();
                    var alt = title.Value.Count > 1 ? string.Join("; ", title.Value.Skip(1).Select(x => x.Translator)) : "";
                    var style = c.Volume % 2 == 0 ? new Style(Color.Grey62) : new Style(Color.White);
                    table.AddRow(
                        new Text($"{c.Volume}", style),
                        new Text($"{c.Chapter}", style),
                        new Text(title.Key, style),
                        new Text(c.Translator, style),
                        new Text(alt, style));
                }
                AnsiConsole.Write(table);
            }
            else return 1;

            //ScrapService.Instance.Dispose();
            
            return 0;
        }
    }
}