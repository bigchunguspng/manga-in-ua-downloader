using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using MangaInUaDownloader.Commands;
using MangaInUaDownloader.Downloaders;
using MangaInUaDownloader.Model;
using MangaInUaDownloader.Services;
using MangaInUaDownloader.Utils;
using MangaInUaDownloader.Utils.ConsoleExtensions;
using Spectre.Console;
using Spectre.Console.Rendering;
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

            try
            {
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
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"\n[white]{e.GetType()}:[/] [red]{e.Message}[/]\n");
                
                return -1;
            }

            return 0;
        }

        
        private async Task ListAvailableChapters()
        {
            AnsiConsole.MarkupLine("Виконую команду [yellow][[перелік всіх розділів]][/]");
            
            Dictionary<string, List<MangaChapter>> chapters = null!;

            await AnsiConsole.Status().StartAsync("...", async ctx =>
            {
                chapters = await _mangaService.GetChaptersGrouped(URL, new StatusStatus(ctx, "yellow"));
            });
            
            var table = CreateChaptersTable().AddColumn(new TableColumn("ALT"));

            foreach (var title in chapters)
            {
                var chapter = title.Value.First();
                var alts = title.Value.Count > 1 ? string.Join("; ", title.Value.Skip(1).Select(x => x.Translator)) : "";
                var style = chapter.Volume % 2 == 0 ? "yellow" : "blue";
                IRenderable[] row =
                {
                    new Markup($"[{style}]{chapter.Volume}[/]"),
                    new Markup($"[{style}]{chapter.Chapter}[/]"),
                    new Markup($"[{style}]{title.Key}[/]"),
                    new Markup($"[{style}]{chapter.Translator}[/]"),
                    new Markup($"[{style}]{alts}[/]")
                };
                table.AddRow(row);
            }

            AnsiConsole.Write(table);
        }

        private async Task DownloadChapters()
        {
            AnsiConsole.MarkupLine($"Виконую команду [yellow][[{(ListSelected ? "перелік" : "завантаження")} декількох розділів]][/]");
            
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

            if (chapters.Count == 0)
            {
                AnsiConsole.MarkupLine("\n[yellow]За вашим запитом не знайдено жодного розділу манґи, спробуйте прибрати зайві опції.\n[/]");
                return;
            }
            
            var table = CreateChaptersTable();
            
            foreach (var chapter in chapters)
            {
                IRenderable[] row =
                {
                    new Text($"{chapter.Volume}"),
                    new Text($"{chapter.Chapter}"),
                    new Text(chapter.Title),
                    new Text(chapter.Translator)
                };
                table.AddRow(row);
            }
            AnsiConsole.MarkupLine($"\nЗа вашим запитом знайдено [blue]{chapters.Count} розділ{Ending_UKR(chapters.Count)}[/] манґи [yellow]\"{title}\"[/]:");
            AnsiConsole.Write(table);

            if (ListSelected) return;


            var root = MakeDirectory ? Directory.CreateDirectory(title).FullName : Environment.CurrentDirectory;

            await GetChapterDownloadingProgress().StartAsync(async ctx =>
            {
                AnsiConsole.MarkupLine($"Розпочинаю завантаження до {(MakeDirectory ? $"теки [yellow]\"[link]{root}[/]\"[/]" : "поточної теки")}.");

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
            AnsiConsole.MarkupLine("Виконую команду [yellow][[завантаження одного розділу]][/]");
            
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
            return AnsiConsole.Progress().Columns(new TaskNameColumn(), new ProgressBarColumn() { CompletedStyle = Color.Olive }, new PagesDownloadedColumn(), new SpinnerColumn(), new TaskStatusColumn());
        }

        private ProgressTask NewChapterProgressTask(ProgressContext ctx, MangaChapter chapter)
        {
            return ctx.AddTask($"Том {chapter.Volume}. Розділ {chapter.Chapter}:", maxValue: double.NaN);
        }


        private string Ending_UKR(int n)
        {
            if (n      is >= 5 and <= 20) return "ів";
            if (n % 10 == 1)              return "";
            if (n % 10 is >= 2 and <=  4) return "и";

            return "ів";
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