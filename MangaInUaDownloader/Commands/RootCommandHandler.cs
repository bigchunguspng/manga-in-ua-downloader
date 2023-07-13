using System.CommandLine.Invocation;
using MangaInUaDownloader.Services;
using MangaInUaDownloader.Utils;
using Spectre.Console;
using Range = MangaInUaDownloader.Utils.Range;

namespace MangaInUaDownloader.Commands
{
    public class RootCommandHandler : ICommandHandler
    {
        private float Chapter, FromChapter, ToChapter;
        private int Volume, FromVolume, ToVolume;
        private bool Chapterize;
        private string? Translator;
        private bool DownloadOtherTranslators;
        private bool ListTranslators, ListChapters;
        private Uri? URL;

        private readonly MangaService _mangaService;

        public RootCommandHandler(MangaService mangaService)
        {
            _mangaService = mangaService;
        }

        public int Invoke(InvocationContext context) => InvokeAsync(context).Result;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            URL = context.ParseResult.GetValueForArgument(RootCommandBuilder.URLArg);

            Chapter = context.ParseResult.GetValueForOption(RootCommandBuilder.ChapterOption);
            FromChapter = context.ParseResult.GetValueForOption(RootCommandBuilder.FromChapterOption);
            ToChapter = context.ParseResult.GetValueForOption(RootCommandBuilder.ToChapterOption);
            Volume = context.ParseResult.GetValueForOption(RootCommandBuilder.VolumeOption);
            FromVolume = context.ParseResult.GetValueForOption(RootCommandBuilder.FromVolumeOption);
            ToVolume = context.ParseResult.GetValueForOption(RootCommandBuilder.ToVolumeOption);

            Chapterize = context.ParseResult.GetValueForOption(RootCommandBuilder.ChapterizeOption);
            ListTranslators = context.ParseResult.GetValueForOption(RootCommandBuilder.ListTranslatorsOption);
            ListChapters = context.ParseResult.GetValueForOption(RootCommandBuilder.ListChaptersOption);

            var ot = context.ParseResult.GetValueForOption(RootCommandBuilder.OnlyTranslatorOption);
            var pt = context.ParseResult.GetValueForOption(RootCommandBuilder.PreferTranslatorOption);
            Translator = ot ?? pt;
            DownloadOtherTranslators = ot is null;

            if (ListTranslators)
            {
                var chapters = await _mangaService.GetTranslatorsByChapter(URL);
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
                    //var chap = x.ChapterA.Equals(x.ChapterB) ? $"{x.ChapterA}" : $"{x.ChapterA} - {x.ChapterB}";
                    //Console.WriteLine($"{chap}: {string.Join(" | ", x.Translators)}");
                }
                AnsiConsole.Write(table);

                return 0;
            }
            else
            {
                var c = Chapter < 0 ? new RangeF(FromChapter, ToChapter) : new RangeF(Chapter, Chapter);
                var v = Volume  < 0 ? new Range (FromVolume,  ToVolume ) : new Range (Volume,  Volume );

                var chapters = (await _mangaService.GetChapters(URL, c, v, Translator, DownloadOtherTranslators)).ToList();
                foreach (var chapter in chapters)
                {
                    Console.WriteLine($"Vol. {chapter.Volume} Ch. {chapter.Chapter} {chapter.Title} (by {chapter.Translator}{(chapter.IsAlternative ? " (ALT)" : "")})");
                }

                if (!ListChapters)
                {
                    var downloader = new Downloader(Chapterize);
                    await downloader.DownloadChapters(chapters);
                }
            }
            
            ScrapService.Instance.Dispose();
            
            return 2;
        }
    }
}