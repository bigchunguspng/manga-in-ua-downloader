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
        
        public int Chapter, FromChapter, ToChapter;
        public int Volume, FromVolume, ToVolume;
        public string? Translator;
        public bool DownloadOtherTranslators;
        public bool ListTranslators;
        public Uri URL;

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
                    var chap = x.ChapterA.Equals(x.ChapterB) ? x.ChapterA.ToString() : $"{x.ChapterA} - {x.ChapterB}";
                    Console.WriteLine($"{chap}: {string.Join(" | ", x.Translators)}");
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
}