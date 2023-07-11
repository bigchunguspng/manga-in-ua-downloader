using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

namespace MangaInUaDownloader.Commands
{
    public static class RootCommandBuilder
    {
        public static readonly RootCommand Root = new("Command line tool to download manga from manga.in.ua");

        public static readonly Option<int>     ChapterOption = new("--chapter", "Chapter number.");
        public static readonly Option<int> FromChapterOption = new("--from-chapter", "Number of the first chapter to be downloaded.") {ArgumentHelpName = "chapter"};
        public static readonly Option<int>   ToChapterOption = new("--to-chapter", "Number of the last chapter to be downloaded.") {ArgumentHelpName = "chapter"};
            
        public static readonly Option<int>     VolumeOption = new("--volume", "Volume number.");
        public static readonly Option<int> FromVolumeOption = new("--from-volume", "Number of the first volume to be downloaded.") {ArgumentHelpName = "volume"};
        public static readonly Option<int>   ToVolumeOption = new("--to-volume", "Number of the last volume to be downloaded.") {ArgumentHelpName = "volume"};
            
        public static readonly Option<string>   OnlyTranslatorOption = new("--only-translator", "Download only chapters translated by that translator.") {ArgumentHelpName = "name"};
        public static readonly Option<string> PreferTranslatorOption = new("--prefer-translator", "Choose chapters translated by that translator if there is a choice.") { ArgumentHelpName = "name" };

        public static readonly Option<bool> ListTranslatorsOption = new("--list-translators", "Show who translated which chapters.");

        //var urlOption = new Option<Uri>("--URL", "Link to the manga, e.g: https://manga.in.ua/mangas/...html.") { IsRequired = true };
        public static readonly Argument<Uri> URLArg = new("Link to the manga, e.g: https://manga.in.ua/mangas/...html.");

        public static RootCommand Build()
        {
            Root.Add(ChapterOption);
            Root.Add(FromChapterOption);
            Root.Add(ToChapterOption);
            Root.Add(VolumeOption);
            Root.Add(FromVolumeOption);
            Root.Add(ToVolumeOption);
            Root.Add(OnlyTranslatorOption);
            Root.Add(PreferTranslatorOption);
            Root.Add(ListTranslatorsOption);
            //root.Add(urlOption);
            Root.AddArgument(URLArg);

            var handler = new RootCommandHandler(new MangaService());

            //Root.Handler = new RootCommandHandler(new MangaService());
            Root.SetHandler(handler.InvokeAsync);

            return Root;
        }
    }
}