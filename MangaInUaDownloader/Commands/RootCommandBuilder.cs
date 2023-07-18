using System.CommandLine;
using System.CommandLine.Invocation;

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

        public static readonly Option<bool>  DirectoryOption = new("--directory", "Create a new folder for this title."); // todo replace with UseThisDir
        public static readonly Option<bool> ChapterizeOption = new("--chapterize", "Create a separate folder for each chapter.");
            
        public static readonly Option<string>   OnlyTranslatorOption = new("--only-translator", "Download only chapters translated by that translator.") { ArgumentHelpName = "name" };
        public static readonly Option<string> PreferTranslatorOption = new("--prefer-translator", "Choose chapters translated by that translator if there is a choice.") { ArgumentHelpName = "name" };

        public static readonly Option<bool> ListChaptersOption = new("--list-chapters", "Show all chapters.");
        public static readonly Option<bool> ListSelectedOption = new("--list-selected", "Show chapters selected by given query (DEBUG OPTION).");

        public static readonly Argument<Uri> URLArg = new("Manga or chapter URL, e.g: https://manga.in.ua/...html.");

        private static void AddAliases()
        {
            ChapterOption.AddAlias("-c");
            FromChapterOption.AddAlias("-fc");
            ToChapterOption.AddAlias("-tc");
            
            VolumeOption.AddAlias("-v");
            FromVolumeOption.AddAlias("-fv");
            ToVolumeOption.AddAlias("-tv");
            
            DirectoryOption.AddAlias("-d");
            
            ChapterizeOption.AddAlias("-s");
            
            OnlyTranslatorOption.AddAlias("-ot");
            PreferTranslatorOption.AddAlias("-pt");
            
            ListChaptersOption.AddAlias("-lc");
            ListSelectedOption.AddAlias("-ls");
        }

        public static RootCommand Build(ICommandHandler handler)
        {
            AddAliases();
            
            Root.Add(ChapterOption);
            Root.Add(FromChapterOption);
            Root.Add(ToChapterOption);
            Root.Add(VolumeOption);
            Root.Add(FromVolumeOption);
            Root.Add(ToVolumeOption);
            Root.Add(DirectoryOption);
            Root.Add(ChapterizeOption);
            Root.Add(OnlyTranslatorOption);
            Root.Add(PreferTranslatorOption);
            Root.Add(ListChaptersOption);
            Root.Add(ListSelectedOption);

            Root.AddArgument(URLArg);

            Root.SetHandler(handler.InvokeAsync);

            return Root;
        }
    }
}