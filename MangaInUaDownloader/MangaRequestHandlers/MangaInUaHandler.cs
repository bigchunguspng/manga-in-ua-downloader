using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using MangaInUaDownloader.Commands;
using MangaInUaDownloader.Downloaders;
using MangaInUaDownloader.Model;
using MangaInUaDownloader.Services;
using MangaInUaDownloader.Utils;
using MangaInUaDownloader.Utils.ConsoleExtensions;
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
        private string URL = null!;

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

            MakeDirectory = !context.ParseResult.GetValueForOption(RootCommandBuilder.DirectoryOption);
            Chapterize = context.ParseResult.GetValueForOption(RootCommandBuilder.ChapterizeOption);
            ListChapters = context.ParseResult.GetValueForOption(RootCommandBuilder.ListChaptersOption);
            ListSelected = context.ParseResult.GetValueForOption(RootCommandBuilder.ListSelectedOption);

            var ot = context.ParseResult.GetValueForOption(RootCommandBuilder.OnlyTranslatorOption);
            var pt = context.ParseResult.GetValueForOption(RootCommandBuilder.PreferTranslatorOption);
            Translator = ot ?? pt;
            DownloadOtherTranslators = ot is null;
            
            if      (_mangaService.IsChapterURL(URL))
            {
                await DownloadSingleChapter();
            }
            else if (_mangaService.IsMangaURL(URL))
            {
                if (ListChapters) await ListAvailableChapters();
                else              await DownloadChapters();
            }
            else return 1;

            return 0;
        }

        
        private async Task ListAvailableChapters()
        {
            var chapters = await _mangaService.GetChaptersGrouped(URL, new ConsoleStatus());
            var table = CreateChaptersTable().AddColumn(new TableColumn("ALT"));
            foreach (var title in chapters)
            {
                var c = title.Value.First();
                var alt = title.Value.Count > 1 ? string.Join("; ", title.Value.Skip(1).Select(x => x.Translator)) : "";
                var style = c.Volume % 2 == 0 ? new Style(Color.Yellow) : new Style(Color.DeepSkyBlue1);
                table.AddRow(
                    new Text($"{c.Volume}", style),
                    new Text($"{c.Chapter}", style),
                    new Text(title.Key, style),
                    new Text(c.Translator, style),
                    new Text(alt, style));
            }

            AnsiConsole.Write(table);
        }

        private async Task DownloadChapters()
        {
            List<MangaChapter> chapters = null!;

            await AnsiConsole.Status().StartAsync("...", async ctx =>
            {
                var status = new StatusStatus(ctx, "yellow");

                var c = Chapter < 0 ? new RangeF(FromChapter, ToChapter) : new RangeF(Chapter, Chapter);
                var v = Volume  < 0 ? new Range (FromVolume,  ToVolume ) : new Range (Volume,  Volume );

                var options = new MangaDownloadOptions(c, v, Translator, DownloadOtherTranslators);

                chapters = (await _mangaService.GetChapters(URL, status, options)).ToList();
            });

            var title = await _mangaService.GetMangaTitle(URL, new SilentStatus());

            var table = CreateChaptersTable();
            
            foreach (var chapter in chapters)
            {
                table.AddRow(
                    new Text($"{chapter.Volume }" ),
                    new Text($"{chapter.Chapter}" ),
                    new Text(   chapter.Title     ),
                    new Text(   chapter.Translator));
            }
            AnsiConsole.MarkupLine($"\nЗа вашим запитом буде завантажено [blue]{chapters.Count}[/] розділів манґи [yellow]\"{title}\"[/]:"); // todo if 0 TEST!!!
            AnsiConsole.Write(table);

            if (ListSelected) return;


            var root = MakeDirectory ? Directory.CreateDirectory(title).FullName : Environment.CurrentDirectory;

            await GetChapterDownloadingProgress().StartAsync(async ctx =>
            {
                AnsiConsole.MarkupLine($"Розпочинаю завантаження до {(MakeDirectory ? $"теки [yellow link]\"{root}\"[/]" : "поточної теки")}.");

                var downloading = new List<Task>(chapters.Count);

                foreach (var volume in chapters.GroupBy(x => x.Volume))
                {
                    var vol = Path.Combine(root, VolumeDirectoryName(volume.Key));
                    
                    foreach (var chapter in volume)
                    {
                        var path = Chapterize ? Path.Combine(vol, ChapterDirectoryName(chapter)) : vol;

                        var progress = NewChapterProgressTask(ctx, chapter);
                        var pages = await _mangaService.GetChapterPages(chapter.URL, new ProgressStatus(progress));

                        var download = new RawDownloadTask(pages, path, chapter.Chapter, Chapterize).Run(progress);
                        downloading.Add(download);
                    }
                }

                await Task.WhenAll(downloading);
            });

            AnsiConsole.MarkupLine("[green]Манґа завантажена![/]\n");
        }

        private async Task DownloadSingleChapter()
        {
            MangaChapter chapter = null!;
            List<string> pages   = null!;
            string       path    = null!;

            await AnsiConsole.Status().StartAsync("...", async ctx =>
            {
                var status = new StatusStatus(ctx, "yellow");

                pages   = await _mangaService.GetChapterPages  (URL, status);
                chapter = await _mangaService.GetChapterDetails(URL, status);

                path = VolumeDirectoryName(chapter.Volume);

                if (MakeDirectory) path = Path.Combine(await _mangaService.GetMangaTitle(URL, status), path);
                if (Chapterize)    path = Path.Combine(path, ChapterDirectoryName(chapter));
            });

            await GetChapterDownloadingProgress().StartAsync(async ctx =>
            {
                var progress = NewChapterProgressTask(ctx, chapter);
                await new RawDownloadTask(pages, path, chapter.Chapter, Chapterize).Run(progress);
            });
        }

        private Table CreateChaptersTable()
        {
            return new Table()
                .Border(TableBorder.Simple)
                .BorderColor(Color.White)
                .AddColumn(new TableColumn("VOL").RightAligned())
                .AddColumn(new TableColumn("CH").RightAligned())
                .AddColumn(new TableColumn("TITLE"))
                .AddColumn(new TableColumn("TRANSLATED BY"));
        }
        
        private Progress GetChapterDownloadingProgress()
        {
            return AnsiConsole.Progress().Columns(new TaskNameColumn(), new ProgressBarColumn(), new PagesDownloadedColumn(), new SpinnerColumn(), new TaskStatusColumn());
        }

        private ProgressTask NewChapterProgressTask(ProgressContext ctx, MangaChapter chapter)
        {
            return ctx.AddTask($"Том {chapter.Volume}. Розділ {chapter.Chapter}:", maxValue: double.NaN);
        }
        
        
        private string VolumeDirectoryName(int i) => $"Том {i}";

        private string ChapterDirectoryName(MangaChapter chapter)
        {
            var name = chapter.Title == MangaService.UNTITLED
                ? $"Розділ {chapter.Chapter}"
                : $"Розділ {chapter.Chapter} - {chapter.Title}";

            return RemoveIllegalCharacters(name);
        }

        private static string RemoveIllegalCharacters(string path)
        {
            path = path.Replace("...", "…");
            var chars = Path.GetInvalidFileNameChars();
            return chars.Aggregate(path, (current, c) => current.Replace(c.ToString(), ""));
        }
    }
}