using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using MangaInUaDownloader.Downloaders;
using MangaInUaDownloader.Model;
using MangaInUaDownloader.Services;
using MangaInUaDownloader.Utils;
using MangaInUaDownloader.Utils.ConsoleExtensions;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextCopy;
using Range = MangaInUaDownloader.Utils.Range;
using Root = MangaInUaDownloader.Commands.RootCommandBuilder;

namespace MangaInUaDownloader.MangaRequestHandlers
{
    public class MangaInUaHandler : MangaRequestHandler
    {
        private readonly Regex _url = new(@"^https?:\/\/manga\.in\.ua\/(?:(?:mangas)|(?:chapters))\/\S+");
        
        private readonly IMangaService _mangaService;
        
        private float Chapter, FromChapter, ToChapter;
        private int Volume, FromVolume, ToVolume;
        private bool MakeDirectory, Chapterize, Cbz, Slow;
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

            var query = context.ParseResult.GetValueForArgument(Root.URLArg).ToString();

            List<MangaSearchResult> results = null!;
            AnsiConsole.Status().Start("...", ctx =>
            {
                results = _mangaService.Search(query, DefaultStatus(ctx)).Result;
            });

            var count = results.Count;
            if (count == 0)
            {
                AnsiConsole.MarkupLine("\n[yellow]Нічого не знайдено.[/]\n");
                return;
            }
            
            AnsiConsole.MarkupLine($"\nЗа вашим запитом знайдено [blue]{count}[/] результат{Ending_UKR(count)}:");

            for (var i = 0; i < count; i++)
            {
                var item = results[i];
                var number = count > 1 ? (i + 1).ToString().PadLeft(2, ' ') + ". " : "    ";
                AnsiConsole.MarkupLineInterpolated($"\n{number}{item.TitleUkr} [blue]({item.Progress})[/]");
                AnsiConsole.MarkupLineInterpolated($"    [dim]{item.TitleEng}[/]");
            }

            if (count == 1) await CopyLink(results[0].URL);
            else
            {
                var nothing = "[нічого]".EscapeMarkup();
                var selection = new SelectionPrompt<string>()
                {
                    Title = "[yellow]\nОберіть те, що вас цікавить:[/]",
                    PageSize = 12,
                    MoreChoicesText = "[dim](Прокрутіть вниз, щоб побачити більше варіантів)[/]"
                };
                selection.AddChoices(results.Select(x => x.TitleUkr.EscapeMarkup()));
                selection.AddChoice(nothing);

                var choise = AnsiConsole.Prompt(selection);
                if (choise == nothing)
                {
                    AnsiConsole.MarkupLine("\n[yellow]Розумію...[/]\n");
                }
                else await CopyLink(results.First(x => choise == x.TitleUkr.EscapeMarkup()).URL);
            }

            async Task CopyLink(string url)
            {
                await ClipboardService.SetTextAsync(url);
                AnsiConsole.MarkupLine("\n[yellow]Посилання скопійовано до буферу обміну![/]\n");
            }
        }

        public override async Task<int> InvokeAsync(InvocationContext context)
        {
            var result = context.ParseResult;

            URL = result.GetValueForArgument(Root.URLArg).ToString();

            Chapter     = result.GetValueForOption(Root.ChapterOption);
            FromChapter = result.GetValueForOption(Root.FromChapterOption);
            ToChapter   = result.GetValueForOption(Root.ToChapterOption);
            Volume      = result.GetValueForOption(Root.VolumeOption);
            FromVolume  = result.GetValueForOption(Root.FromVolumeOption);
            ToVolume    = result.GetValueForOption(Root.ToVolumeOption);

            MakeDirectory = !result.GetValueForOption(Root.DirectoryOption);
            Chapterize    =  result.GetValueForOption(Root.ChapterizeOption);
            Slow = result.GetValueForOption(Root.SlowOption);
            Cbz  = result.GetValueForOption(Root.CbzOption);
            ListChapters = result.GetValueForOption(Root.ListChaptersOption);
            ListSelected = result.GetValueForOption(Root.ListSelectedOption);

            var ot = result.GetValueForOption(Root.OnlyTranslatorOption);
            var pt = result.GetValueForOption(Root.PreferTranslatorOption);
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
                AnsiConsole.MarkupLine(ReadableExceptionText(e));
                
                return -1;
            }

            return 0;
        }

        
        private async Task ListAvailableChapters()
        {
            AnsiConsole.MarkupLine("Виконую команду [yellow][[перелік всіх розділів]][/]");
            
            List<List<MangaChapter>> transtalions = null!;
            string title = null!;

            await AnsiConsole.Status().StartAsync("...", async ctx =>
            {
                var status = DefaultStatus(ctx);
                transtalions = await _mangaService.GetTranslations(URL, status);
                title        = await _mangaService.GetMangaTitle  (URL, status);
            });

            var chapters = transtalions.Select(x => x[0]).ToList();

            AnsiConsole.MarkupLine($"\nЗнайдено {N_CHAPTERS_OF_THIS_MANGA(chapters.Count, title)}");
            AnsiConsole.Write(ChaptersFoundTable(chapters, transtalions));
        }

        private async Task DownloadChapters()
        {
            AnsiConsole.MarkupLine($"Виконую команду [yellow][[{(ListSelected ? "перелік" : "завантаження")} декількох розділів]][/]");
            
            List<MangaChapter> chapters = null!;
            string title = null!;

            await AnsiConsole.Status().StartAsync("...", async ctx =>
            {
                var status = DefaultStatus(ctx);
                var options = new MangaDownloadOptions
                (
                    Chapter < 0 ? new RangeF(FromChapter, ToChapter) : new RangeF(Chapter, Chapter),
                    Volume  < 0 ? new Range (FromVolume,  ToVolume ) : new Range (Volume,  Volume ),
                    Translator, DownloadOtherTranslators
                );
                chapters = (await _mangaService.GetChapters  (URL, status, options)).ToList();
                title    =  await _mangaService.GetMangaTitle(URL, status);
            });

            if (chapters.Count == 0)
            {
                AnsiConsole.MarkupLine("\n[yellow]За вашим запитом не знайдено жодного розділу манґи, спробуйте прибрати зайві опції.\n[/]");
                return;
            }

            AnsiConsole.MarkupLine($"\nЗа вашим запитом знайдено {N_CHAPTERS_OF_THIS_MANGA(chapters.Count, title)}");
            AnsiConsole.Write(ChaptersFoundTable(chapters));

            if (ListSelected) return;


            var root = GetRootDirectory(title);

            Exception? exception = null;
            var downloading = new List<Task>(chapters.Count);
            var directories = new HashSet<string>();

            await GetChapterDownloadingProgress().StartAsync(async ctx =>
            {
                AnsiConsole.MarkupLine(DOWNLOAD_START(root));
                ProgressTask progress = null!;
                try
                {
                    foreach (var volume in chapters.GroupBy(x => x.Volume))
                    {
                        var vol = Path.Combine(root, VolumeDirectoryName(volume.Key));

                        foreach (var chapter in volume)
                        {
                            var path = Chapterize ? Path.Combine(vol, ChapterDirectoryName(chapter)) : vol;
                            directories.Add(path);

                            if (Slow) // wait [5 seconds] OR [till the chapter is 75% loaded]
                            {
                                for (var i = 0; i < 10; i++)
                                {
                                    await Task.Delay(500);
                                    if (progress is { Percentage: > 75 }) break;
                                }
                            }

                            progress = NewChapterProgressTask(ctx, chapter);
                            var pages = await _mangaService.GetChapterPages(chapter.URL, new ProgressStatus(progress));

                            DownloadTask downloader = Cbz
                                ? new CbzDownloadTask(CbzFileName(title, chapter), root)
                                : new RawDownloadTask();

                            downloading.Add(downloader.Of(pages, path, chapter.Chapter, Chapterize).Run(progress));
                        }
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                    progress.SetStatus("[darkorange]Terminated ⨯[/]");
                }

                await Task.WhenAll(downloading);

                if (Cbz)
                    foreach (var directory in directories)
                        ClearEmptyDirectories(directory);
            });

            var message = downloading.Count == chapters.Count
                ? "[green]Манґа завантажена![/]\n"
                : $"[darkorange]Манґа завантажена частково.[/] Причина:{EXCEPTION_AS_REASON(exception)}";
            AnsiConsole.MarkupLine(message);
        }

        private async Task DownloadSingleChapter()
        {
            AnsiConsole.MarkupLine("Виконую команду [yellow][[завантаження одного розділу]][/]");
            
            MangaChapter chapter = null!;
            List<string> pages   = null!;
            string       title   = null!;

            await AnsiConsole.Status().StartAsync("...", async ctx =>
            {
                var status = DefaultStatus(ctx);
                pages   = await _mangaService.GetChapterPages  (URL, status);
                chapter = await _mangaService.GetChapterDetails(URL, status);
                title   = await _mangaService.GetMangaTitle    (URL, status);
            });

            var root = GetRootDirectory(title);
            var path = Path.Combine(root, VolumeDirectoryName(chapter.Volume));

            if (Chapterize) path = Path.Combine(path, ChapterDirectoryName(chapter));

            await GetChapterDownloadingProgress().StartAsync(async ctx =>
            {
                AnsiConsole.MarkupLine("\n" + DOWNLOAD_START(root));

                var progress = NewChapterProgressTask(ctx, chapter);

                DownloadTask downloader = Cbz
                    ? new CbzDownloadTask(CbzChapterName(title, chapter), root)
                    : new RawDownloadTask();

                await downloader.Of(pages, path, chapter.Chapter, Chapterize).Run(progress);

                if (Cbz) ClearEmptyDirectories(path);
            });

            AnsiConsole.MarkupLine("[green]Розділ завантажений![/]\n");
        }


        #region USER INTERFACE

        private StatusStatus DefaultStatus(StatusContext ctx) => new(ctx, "yellow");

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

        private string GetChapterRowStyle(List<int> volumes, int volume)
        {
            return volumes.Count == 1
                ? "default"
                : (volumes.IndexOf(volume) % 4) switch
                {
                    0 => "gold1",
                    1 => "khaki1",
                    2 => "gold1",
                    3 => "orange3",
                    _ => throw new ArgumentOutOfRangeException()
                };
        }

        private Table ChaptersFoundTable(List<MangaChapter> chapters, List<List<MangaChapter>>? translationsByChapter = null)
        {
            var altsPassed = translationsByChapter != null;

            var count = chapters.Count;
            var table = CreateChaptersTable();
            if (altsPassed)
            {
                table.AddColumn(new TableColumn("ALT"));
            }

            var volumes = chapters.DistinctBy(c => c.Volume).Select(x => x.Volume).ToList();

            for (var i = 0; i < count; i++)
            {
                var chapter = chapters[i];
                var first = i == 0         || chapters[i - 1].Volume != chapter.Volume;
                var last  = i == count - 1 || chapters[i + 1].Volume != chapter.Volume;

                var style = GetChapterRowStyle(volumes, chapter.Volume);

                List<IRenderable> row = new()
                {
                    new Markup($"[{style}]{GetVolumeBoxPart(chapter.Volume, first, last)}[/]"),
                    new Markup($"[{style}]{chapter.Chapter}[/]"),
                    new Markup($"[{style}]{chapter.Title}[/]"),
                    new Markup($"[{style}]{chapter.Translator}[/]")
                };

                if (altsPassed)
                {
                    var translations = translationsByChapter![i];
                    if (translations.Count > 1)
                    {
                        var alts = translations.Skip(1).Select(x => x.Translator);
                        row.Add(new Markup($"[{style}]{string.Join("; ", alts)}[/]"));
                    }
                }
                table.AddRow(row);
            }

            return table;
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

        #endregion

        #region STRINGS

        private string ReadableExceptionText(Exception e)
        {
            return $"\n[white]{e.GetType().Name}:[/] [red]{e.Message}[/]\n";
        }

        private string EXCEPTION_AS_REASON(Exception? e)
        {
            return e is null ? " невідома\n" : $"\n{ReadableExceptionText(e)}";
        }

        private string N_CHAPTERS_OF_THIS_MANGA(int count, string title)
        {
            return $"[blue]{count} розділ{Ending_UKR(count)}[/] манґи [yellow]\"{title.EscapeMarkup()}\"[/]:";
        }
        
        private string DOWNLOAD_START(string path)
        {
            var dir = MakeDirectory ? $"теки [yellow]\"[link]{path.EscapeMarkup()}[/]\"[/]" : "поточної теки";
            return $"Розпочинаю завантаження до {dir}.";
        }

        private string Ending_UKR(int n)
        {
            n = n % 100;
            if (n      is >= 5 and <= 20) return "ів";
            if (n % 10 == 1)              return "";
            if (n % 10 is >= 2 and <=  4) return "и";

            return "ів";
        }

        #endregion

        #region PATH

        private string GetRootDirectory(string title)
        {
            return MakeDirectory 
                ? Directory.CreateDirectory(RemoveIllegalChars(title)).FullName 
                : Environment.CurrentDirectory;
        }

        private string VolumeDirectoryName(int i) => $"Том {i}";

        private string ChapterDirectoryName(MangaChapter chapter)
        {
            var name = chapter.Title == IMangaService.UNTITLED
                ? $"Розділ {chapter.Chapter}"
                : $"Розділ {chapter.Chapter} - {chapter.Title}";

            return RemoveIllegalChars(name);
        }

        private string CbzChapterName(string title, MangaChapter chapter)
        {
            return $"{RemoveIllegalChars(title)} - Том {chapter.Volume}. Розділ {chapter.Chapter}.cbz";
        }

        private string CbzVolumeName(string title, MangaChapter chapter)
        {
            return $"{RemoveIllegalChars(title)} - Том {chapter.Volume}.cbz";
        }

        private string CbzFileName(string title, MangaChapter chapter)
        {
            return Chapterize ? CbzChapterName(title, chapter) : CbzVolumeName(title, chapter);
        }

        private static string RemoveIllegalChars(string pathPart)
        {
            var chars = Path.GetInvalidFileNameChars();
            return string.Join("", pathPart.Split(chars));
        }

        #endregion

        #region CLEANING

        private void ClearEmptyDirectories(string chapterLocation)
        {
            DeleteEmptyDirectory(chapterLocation);
            if (Chapterize)
                DeleteEmptyDirectory(Path.GetDirectoryName(chapterLocation)!);
        }

        private static void DeleteEmptyDirectory(string path)
        {
            if (DirectoryIsEmpty(path)) Directory.Delete(path);
        }

        private static bool DirectoryIsEmpty(string path)
            => Directory.EnumerateFileSystemEntries(path).Any() == false;

        #endregion
    }
}