using System.CommandLine;

namespace MangaInUaDownloader.Commands
{
    public static class RootCommandBuilder
    {
        private static readonly RootCommand Root = new("A command line tool to download manga from 'Manga.in.ua'");

        public static readonly Option<float>     ChapterOption = new(     "--chapter", () => float.MinValue, "Chapter number.");
        public static readonly Option<float> FromChapterOption = new("--from-chapter", () => float.MinValue, "Number of the first chapter to be downloaded.") { ArgumentHelpName = "chapter" };
        public static readonly Option<float>   ToChapterOption = new(  "--to-chapter", () => float.MaxValue, "Number of the last chapter to be downloaded.") { ArgumentHelpName = "chapter" };
            
        public static readonly Option<int>     VolumeOption = new(     "--volume", () => int.MinValue, "Volume number.");
        public static readonly Option<int> FromVolumeOption = new("--from-volume", () => int.MinValue, "Number of the first volume to be downloaded.") { ArgumentHelpName = "volume" };
        public static readonly Option<int>   ToVolumeOption = new(  "--to-volume", () => int.MaxValue, "Number of the last volume to be downloaded.") { ArgumentHelpName = "volume" };

        public static readonly Option<bool> ChapterizeOption = new("--chapterize", "Create a folder for each chapter.");
            
        public static readonly Option<string>   OnlyTranslatorOption = new("--only-translator", "Download only chapters translated by that translator.") { ArgumentHelpName = "name" };
        public static readonly Option<string> PreferTranslatorOption = new("--prefer-translator", "Choose chapters translated by that translator if there is a choice.") { ArgumentHelpName = "name" };

        public static readonly Option<bool> ListTranslatorsOption = new("--list-translators", "Show who translated which chapters.");

        public static readonly Argument<Uri> URLArg = new("Link to the manga, e.g: https://manga.in.ua/mangas/...html.");

        private static void AddAliases()
        {
            ChapterOption.AddAlias("-c");
            FromChapterOption.AddAlias("-f");
            ToChapterOption.AddAlias("-t");
            
            VolumeOption.AddAlias("-v");
            FromVolumeOption.AddAlias("-F");
            ToVolumeOption.AddAlias("-T");
            
            ChapterizeOption.AddAlias("-s");
            
            OnlyTranslatorOption.AddAlias("-o");
            PreferTranslatorOption.AddAlias("-p");
            ListTranslatorsOption.AddAlias("-l");
        }

        public static RootCommand Build()
        {
            AddAliases();
            
            Root.Add(ChapterOption);
            Root.Add(FromChapterOption);
            Root.Add(ToChapterOption);
            Root.Add(VolumeOption);
            Root.Add(FromVolumeOption);
            Root.Add(ToVolumeOption);
            Root.Add(ChapterizeOption);
            Root.Add(OnlyTranslatorOption);
            Root.Add(PreferTranslatorOption);
            Root.Add(ListTranslatorsOption);

            Root.AddArgument(URLArg);

            var handler = new RootCommandHandler(new MangaService());

            Root.SetHandler(handler.InvokeAsync);

            return Root;
        }
    }
}