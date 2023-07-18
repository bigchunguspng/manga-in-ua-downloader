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
        private bool MakeDirectory, Chapterize;
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

            MakeDirectory = context.ParseResult.GetValueForOption(RootCommandBuilder.DirectoryOption);
            Chapterize = context.ParseResult.GetValueForOption(RootCommandBuilder.ChapterizeOption);
            ListChapters = context.ParseResult.GetValueForOption(RootCommandBuilder.ListChaptersOption);
            ListSelected = context.ParseResult.GetValueForOption(RootCommandBuilder.ListSelectedOption);

            var ot = context.ParseResult.GetValueForOption(RootCommandBuilder.OnlyTranslatorOption);
            var pt = context.ParseResult.GetValueForOption(RootCommandBuilder.PreferTranslatorOption);
            Translator = ot ?? pt;
            DownloadOtherTranslators = ot is null;
            
            if    (_mangaService.IsChapterURL(URL))
            {
                // call service to get a list of pages urls (and a title if ness)
                var pages = await _mangaService.GetChapterPages(URL);
                var chapter = await _mangaService.GetChapterDetails(URL);
                
                // prepare the path
                var path = VolumeDirectoryName(chapter.Volume);
                if (MakeDirectory) // /Title/Том 1/...
                {
                    var title = await _mangaService.GetMangaTitle(URL);
                    path = Path.Combine(title, path);
                }
                if (Chapterize) // ../Том 1/Розділ 8/... 
                {
                    path = Path.Combine(path, ChapterDirectoryName(chapter));
                }
                
                // pass the shit to dl
                var task = new RawDownloadTask(pages, path, chapter.Chapter, Chapterize);
                await task.Run();
            }
            else if (_mangaService.IsMangaURL(URL) && !ListChapters)
            {
                var c = Chapter < 0 ? new RangeF(FromChapter, ToChapter) : new RangeF(Chapter, Chapter);
                var v = Volume  < 0 ? new Range (FromVolume,  ToVolume ) : new Range (Volume,  Volume );

                var options = new MangaDownloadOptions(c, v, Translator, DownloadOtherTranslators);
                var chapters = (await _mangaService.GetChapters(URL, options)).ToList();

                foreach (var chapter in chapters.ToList()) // cw
                {
                    Console.WriteLine($"Vol. {chapter.Volume} Ch. {chapter.Chapter} {chapter.Title} (by {chapter.Translator}{(chapter.IsAlternative ? " (ALT)" : "")})");
                }

                if (ListSelected) return 0;
                
                // path
                var root = MakeDirectory ? await _mangaService.GetMangaTitle(URL) : "";

                var downloading = new List<Task>(chapters.Count);
                // get each chapter pages and run the task
                var volumes = chapters.GroupBy(x => x.Volume);
                foreach (var volume in volumes)
                {
                    var vol = Path.Combine(root, VolumeDirectoryName(volume.Key));
                    foreach (var chapter in volume)
                    {
                        var path = Path.Combine(vol, ChapterDirectoryName(chapter));
                        var pages = await _mangaService.GetChapterPages(chapter.URL);
                        var task = new RawDownloadTask(pages, path, chapter.Chapter, Chapterize).Run();
                        downloading.Add(task);
                    }
                }

                // await all tasks
                await Task.WhenAll(downloading);
                // cw some shit idk
                Console.WriteLine("done!");
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

            return 0;
        }
        
        private string VolumeDirectoryName(int i) => $"Том {i}";

        private string ChapterDirectoryName(MangaChapter chapter)
        {
            var name = chapter.Title == MangaService.UNTITLED
                ? $"Розділ {chapter.Chapter}"
                : $"Розділ {chapter.Chapter} - {chapter.Title}";

            return ReplaceIllegalCharacters(name);
        }

        private static string ReplaceIllegalCharacters(string path, char x = '#')
        {
            var chars = Path.GetInvalidFileNameChars();
            return chars.Aggregate(path, (current, c) => current.Replace(c, x));
        }
    }
}