using System.CommandLine;
using System.CommandLine.Invocation;

namespace MangaInUaDownloader.Commands
{
    public static class RootCommandBuilder
    {
        private const string _chapter = "розділ", _volume = "том", _nick = "нік";

        private static readonly Command Root = new("MiUD", "Я завантажую [yellow]манґу[/] з сайту [deeppink3]https://manga.in.ua[/]");

        public static readonly Option<float>     ChapterOption = new(     "--chapter", () => float.MinValue, "Розділ, який слід завантажити.") { ArgumentHelpName = _chapter };
        public static readonly Option<float> FromChapterOption = new("--from-chapter", () => float.MinValue, "Перший розділ, що слід завантажити.") { ArgumentHelpName = _chapter };
        public static readonly Option<float>   ToChapterOption = new(  "--to-chapter", () => float.MaxValue, "Останній розділ, що слід завантажити.") { ArgumentHelpName = _chapter };

        public static readonly Option<int>     VolumeOption = new(     "--volume", () => int.MinValue, "Том, розділи з якого слід завантажити.") { ArgumentHelpName = _volume };
        public static readonly Option<int> FromVolumeOption = new("--from-volume", () => int.MinValue, "Перший том, що слід завантажити.") { ArgumentHelpName = _volume };
        public static readonly Option<int>   ToVolumeOption = new(  "--to-volume", () => int.MaxValue, "Останній том, що слід завантажити.") { ArgumentHelpName = _volume };

        public static readonly Option<bool>  DirectoryOption = new("--directory", "Завантажує томи манґи до поточної директорії.");
        public static readonly Option<bool> ChapterizeOption = new("--chapterize", "Зберігає вміст кожного розділу в окрему папку.");

        public static readonly Option<string>   OnlyTranslatorOption = new("--only-translator", "Обирає лише розділи з певним перекладом.") { ArgumentHelpName = _nick };
        public static readonly Option<string> PreferTranslatorOption = new("--prefer-translator", "Надає перевагу розділам з певним перекладом.") { ArgumentHelpName = _nick };

        public static readonly Option<bool> ListChaptersOption = new("--list-chapters", "Перелічує всі розділи, що є на сайті (без завантаження).");
        public static readonly Option<bool> ListSelectedOption = new("--list-selected", "Перелічує всі розділи, що відповідають запиту (без завантаження).");

        public static readonly Argument<Uri> URLArg = new("URL", "Посилання на [yellow]сторінку манґи чи її розділ[/], на зразок цього: [deeppink3]https://manga.in.ua/….html.[/]");

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
            
            OnlyTranslatorOption.AddAlias("-o");
            PreferTranslatorOption.AddAlias("-p");
            
            ListChaptersOption.AddAlias("-lc");
            ListSelectedOption.AddAlias("-ls");
        }

        public static Command Build(ICommandHandler handler)
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