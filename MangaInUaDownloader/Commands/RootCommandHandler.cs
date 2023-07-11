using System.CommandLine.Invocation;

namespace MangaInUaDownloader.Commands
{
    public class RootCommandHandler : ICommandHandler
    {
        /*public int Chapter { get; set; }
        public int FromChapter { get; set; }
        public int ToChapter { get; set; }
        public int Volume { get; set; }
        public int FromVolume { get; set; }
        public int ToVolume { get; set; }

        public string OnlyTranslator { get; set; }
        public string PreferTranslator { get; set; }

        public bool TranslatorsList { get; set; }

        public Uri URL { get; set; }*/
        
        private float Chapter, FromChapter, ToChapter;
        private int Volume, FromVolume, ToVolume;
        private string? Translator;
        private bool DownloadOtherTranslators;
        private bool ListTranslators;
        private Uri URL;

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
            
            ListTranslators = context.ParseResult.GetValueForOption(RootCommandBuilder.ListTranslatorsOption);

            var ot = context.ParseResult.GetValueForOption(RootCommandBuilder.OnlyTranslatorOption);
            var pt = context.ParseResult.GetValueForOption(RootCommandBuilder.PreferTranslatorOption);
            Translator = ot ?? pt;
            DownloadOtherTranslators = ot is null;

            if (ListTranslators)
            {
                var translators = await _mangaService.ListTranslators(URL);
                foreach (var x in translators)
                {
                    var chap = x.ChapterA.Equals(x.ChapterB) ? $"{x.ChapterA}" : $"{x.ChapterA} - {x.ChapterB}";
                    Console.WriteLine($"{chap}: {string.Join(" | ", x.Translators)}");
                }

                return 0;
            }
            else
            {
                var c = Chapter < 0 ? new RangeF(FromChapter, ToChapter) : new RangeF(Chapter, Chapter);
                var v = Volume  < 0 ? new Range (FromVolume,  ToVolume ) : new Range (Volume,  Volume );

                var chapters = await _mangaService.GetChapters(URL, c, v, Translator, DownloadOtherTranslators);
                foreach (var chapter in chapters)
                {
                    Console.WriteLine($"Vol. {chapter.Volume} Ch. {chapter.Chapter} {chapter.Title} (by {chapter.Translator}{(chapter.IsAlternative ? " (ALT)" : "")})");
                }
            }

            //Console.WriteLine(Chapter);
            //Console.WriteLine(FromChapter);
            //Console.WriteLine(Translator);
            //Console.WriteLine(ListTranslators);
            //Console.WriteLine(URL.ToString());

            return 2;
        }
    }

    public struct Range
    {
        public int Min, Max;

        public Range(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    public struct RangeF
    {
        public float Min, Max;

        public RangeF(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}