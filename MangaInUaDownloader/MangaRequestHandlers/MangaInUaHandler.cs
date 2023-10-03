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
using TextCopy;
using Range = MangaInUaDownloader.Utils.Range;

namespace MangaInUaDownloader.MangaRequestHandlers
{
    public class MangaInUaHandler : MangaRequestHandler
    {
        private readonly Regex _url = new(@"^https?:\/\/manga\.in\.ua\/(?:(?:mangas)|(?:chapters))\/\S+");
        
        private readonly IMangaService _mangaService;
        
        private float Chapter, FromChapter, ToChapter;
        private int Volume, FromVolume, ToVolume;
        private bool MakeDirectory, Chapterize;
        private string? Translator;
        private bool DownloadOtherTranslators;
        private bool ListChapters, ListSelected;
        private string URL = null!;

        public MangaInUaHandler(IMangaService mangaService)
        {
            _mangaService = mangaService;
        }

        public override string MANGA_WEBSITE { get; } = "https://manga.in.ua";

        public override bool CanHandleThis(string url) => _url.IsMatch(url);

        public override async Task SearchAsync(InvocationContext context)
        {
            AnsiConsole.MarkupLine("Виконую команду [yellow][[пошук манґи]][/]");

            var query = context.ParseResult.GetValueForArgument(RootCommandBuilder.URLArg).ToString();

            List<MangaSearchResult> result = null!;
            AnsiConsole.Status().Start("...", ctx => { result = _mangaService.Search(query, new StatusStatus(ctx, "yellow")).Result; });

            var count = result.Count;
            if (count == 0)
            {
                AnsiConsole.MarkupLine("\n[yellow]Нічого не знайдено.[/]\n");
                return;
            }
            
            AnsiConsole.MarkupLine($"\nЗа вашим запитом знайдено [blue]{count}[/] результат{Ending_UKR(count)}:");

            for (var i = 0; i < count; i++)
            {
                var item = result[i];
                var number = count > 1 ? (i + 1).ToString().PadLeft(2, ' ') + ". " : "    ";
                AnsiConsole.MarkupLineInterpolated($"\n{number}{item.TitleUkr} [blue]({item.Progress})[/]");
                AnsiConsole.MarkupLineInterpolated($"    [dim]{item.TitleEng}[/]");
            }

            if (count == 1) await CopyLink(result[0].URL);
            else
            {
                var selection = new SelectionPrompt<string>()
                {
                    Title = "[yellow]\nОберіть те, що вас цікавить:[/]",
                    PageSize = 12,
                    MoreChoicesText = "[dim](Прокрутіть вниз, щоб побачити більше варіантів)[/]"
                };
                selection.AddChoices(result.Select(x => x.TitleUkr.EscapeMarkup()));

                var choise = AnsiConsole.Prompt(selection);

                await CopyLink(result.First(x => x.TitleUkr.EscapeMarkup() == choise).URL);
            }

            async Task CopyLink(string url)
            {
                await ClipboardService.SetTextAsync(url);
                AnsiConsole.MarkupLine("\n[yellow]Посилання скопійовано до буферу обміну![/]\n");
            }
        }

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
                AnsiConsole.MarkupLine($"\n[white]{e.GetType().Name}:[/] [red]{e.Message}[/]\n");
                
                return -1;
            }

            return 0;
        }

        
        private async Task ListAvailableChapters()
        {
            AnsiConsole.MarkupLine("Виконую команду [yellow][[перелік всіх розділів]][/]");
            
            List<List<MangaChapter>> chapters = null!;

            await AnsiConsole.Status().StartAsync("...", async ctx =>
            {
                chapters = await _mangaService.GetTranslations(URL, new StatusStatus(ctx, "yellow"));
            });

            var title = await _mangaService.GetMangaTitle(URL, new FakeStatus());
            var table = CreateChaptersTable().AddColumn(new TableColumn("ALT"));

            var volumes = chapters.DistinctBy(c => c[0].Volume).Count();
            var count = chapters.Count;
            for (var i = 0; i < count; i++)
            {
                var translations = chapters[i];
                var chapter  = translations[0];

                var first = i == 0         || chapters[i - 1][0].Volume != chapter.Volume;
                var last  = i == count - 1 || chapters[i + 1][0].Volume != chapter.Volume;

                var alts = translations.Count > 1 ? string.Join("; ", translations.Skip(1).Select(x => x.Translator)) : "";
                var style = GetChapterRowStyle(volumes, chapter.Volume);

                IRenderable[] row =
                {
                    new Markup($"[{style}]{GetVolumeBoxPart(chapter.Volume, first, last)}[/]"),
                    new Markup($"[{style}]{chapter.Chapter}[/]"),
                    new Markup($"[{style}]{chapter.Title}[/]"),
                    new Markup($"[{style}]{chapter.Translator}[/]"),
                    new Markup($"[{style}]{alts}[/]")
                };
                table.AddRow(row);
            }

            AnsiConsole.MarkupLine($"\nЗнайдено [blue]{chapters.Count} розділ{Ending_UKR(chapters.Count)}[/] манґи [yellow]\"{title.EscapeMarkup()}\"[/]:");
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

            var title = await _mangaService.GetMangaTitle(URL, new FakeStatus());

            var count = chapters.Count;
            if (count == 0)
            {
                AnsiConsole.MarkupLine("\n[yellow]За вашим запитом не знайдено жодного розділу манґи, спробуйте прибрати зайві опції.\n[/]");
                return;
            }

            var volumes = chapters.DistinctBy(c => c.Volume).Count();
            var table = CreateChaptersTable();

            for (var i = 0; i < count; i++)
            {
                var chapter = chapters[i];
                var first = i == 0         || chapters[i - 1].Volume != chapter.Volume;
                var last  = i == count - 1 || chapters[i + 1].Volume != chapter.Volume;

                var style = GetChapterRowStyle(volumes, chapter.Volume);

                IRenderable[] row =
                {
                    new Markup($"[{style}]{GetVolumeBoxPart(chapter.Volume, first, last)}[/]"),
                    new Markup($"[{style}]{chapter.Chapter}[/]"),
                    new Markup($"[{style}]{chapter.Title}[/]"),
                    new Markup($"[{style}]{chapter.Translator}[/]")
                };
                table.AddRow(row);
            }
            AnsiConsole.MarkupLine($"\nЗа вашим запитом знайдено [blue]{chapters.Count} розділ{Ending_UKR(chapters.Count)}[/] манґи [yellow]\"{title.EscapeMarkup()}\"[/]:");
            AnsiConsole.Write(table);

            if (ListSelected) return;


            var directory = RemoveIllegalCharacters(title);
            var root = MakeDirectory ? Directory.CreateDirectory(directory).FullName : Environment.CurrentDirectory;

            await GetChapterDownloadingProgress().StartAsync(async ctx =>
            {
                AnsiConsole.MarkupLine($"Розпочинаю завантаження до {(MakeDirectory ? $"теки [yellow]\"[link]{root.EscapeMarkup()}[/]\"[/]" : "поточної теки")}.");

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


        private string GetVolumeBoxPart(int number, bool first, bool last)
        {
            if (first)
            {
                var s = number.ToString();
                return last
                    ? s.PadLeft(3, '─').PadRight(4, '─')
                    : s.PadLeft(2, '─').PadRight(3, '┐').PadLeft(4, '┌');
            }
            if (!last) return "│  │";
            else       return "└──┘";
        }

        private string GetChapterRowStyle(int volumes, int volume)
        {
            return volumes == 1
                ? "default"
                : (volume % 4) switch
                {
                    0 => "gold1",
                    1 => "khaki1",
                    2 => "gold1",
                    3 => "orange3",
                    _ => throw new ArgumentOutOfRangeException()
                };
        }

        private Table CreateChaptersTable()
        {
            return new Table().BorderColor(Color.White).Border(TableBorder.Simple)
                .AddColumn(new TableColumn("VOL").RightAligned())
                .AddColumn(new TableColumn("CH" ).RightAligned())
                .AddColumn(new TableColumn("TITLE"))
                .AddColumn(new TableColumn("TRANSLATED BY"));
        }

        private Progress GetChapterDownloadingProgress()
        {
            var columns = new ProgressColumn[]
            {
                new TaskNameColumn(),
                new ProgressBarColumn() { CompletedStyle = Color.Olive },
                new PagesDownloadedColumn(),
                new SpinnerColumn(),
                new TaskStatusColumn()
            };
            return AnsiConsole.Progress().Columns(columns);
        }

        private ProgressTask NewChapterProgressTask(ProgressContext ctx, MangaChapter chapter)
        {
            return ctx.AddTask($"Том {chapter.Volume}. Розділ {chapter.Chapter}:", maxValue: double.NaN);
        }


        private string Ending_UKR(int n)
        {
            n = n % 100;
            if (n      is >= 5 and <= 20) return "ів";
            if (n % 10 == 1)              return "";
            if (n % 10 is >= 2 and <=  4) return "и";

            return "ів";
        }

        private string VolumeDirectoryName(int i) => $"Том {i}";

        private string ChapterDirectoryName(MangaChapter chapter)
        {
            var name = chapter.Title == IMangaService.UNTITLED
                ? $"Розділ {chapter.Chapter}"
                : $"Розділ {chapter.Chapter} - {chapter.Title}";

            return RemoveIllegalCharacters(name);
        }

        private static string RemoveIllegalCharacters(string path)
        {
            var chars = Path.GetInvalidFileNameChars();
            return chars.Aggregate(path, (current, c) => current.Replace(c.ToString(), ""));
        }
    }
}