using System.CommandLine;
using System.CommandLine.Invocation;

namespace MangaInUaDownloader.Commands
{
    public static class RootCommandBuilder
    {
        private const string _chapter = "розділ", _volume = "том", _nick = "нік";

        private static readonly Command Root = new("MiUD", "[bold]Ця програма[/] завантажує [yellow]манґу[/] з сайту [deeppink3]https://manga.in.ua[/]");

        public static readonly Option<float>     ChapterOption = new(     "--chapter", () => float.MinValue, "Розділ, що слід завантажити.") { ArgumentHelpName = _chapter };
        public static readonly Option<float> FromChapterOption = new("--from-chapter", () => float.MinValue, "Перший розділ, що слід завантажити.") { ArgumentHelpName = _chapter };
        public static readonly Option<float>   ToChapterOption = new(  "--to-chapter", () => float.MaxValue, "Останній розділ, що слід завантажити.\n") { ArgumentHelpName = _chapter };

        public static readonly Option<int>     VolumeOption = new(     "--volume", () => int.MinValue, "Том, розділи якого слід завантажити.") { ArgumentHelpName = _volume };
        public static readonly Option<int> FromVolumeOption = new("--from-volume", () => int.MinValue, "Перший том, що слід завантажити.") { ArgumentHelpName = _volume };
        public static readonly Option<int>   ToVolumeOption = new(  "--to-volume", () => int.MaxValue, "Останній том, що слід завантажити.\n") { ArgumentHelpName = _volume };

        public static readonly Option<bool>  DirectoryOption = new("--directory", "Завантажує томи манґи до поточної директорії.");
        public static readonly Option<bool> ChapterizeOption = new("--chapterize", "Зберігає вміст кожного розділу до окремої теки.\n");

        public static readonly Option<bool>        CbzOption = new("--cbz",  "Зберігає манґу у форматі \".cbz\".");
        public static readonly Option<bool>       SlowOption = new("--slow", "Завантажує розділи один за одним [dim](повільніше)[/]\n");

        public static readonly Option<string>   OnlyTranslatorOption = new("--only-translator", "Обирає лише розділи з певним перекладом.") { ArgumentHelpName = _nick };
        public static readonly Option<string> PreferTranslatorOption = new("--prefer-translator", "Надає перевагу розділам з певним перекладом.\n") { ArgumentHelpName = _nick };

        public static readonly Option<bool> ListChaptersOption = new("--list-chapters", "Перелічує всі розділи, що є на сайті. [dim](без завантаження)[/]");
        public static readonly Option<bool> ListSelectedOption = new("--list-selected", "Перелічує всі розділи, що відповідають запиту. [dim](без завантаження)[/]\n");

        public static readonly Option<bool> SearchOption = new("--search", "Здійснює пошук манґи. [dim](URL не потрібен)[/]\n") { ArgumentHelpName = "пошуковий запит" };

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
            ChapterizeOption.AddAlias("-cp");

            CbzOption.AddAlias("-z");
            SlowOption.AddAlias("-w");
            
            OnlyTranslatorOption.AddAlias("-o");
            PreferTranslatorOption.AddAlias("-p");
            
            ListChaptersOption.AddAlias("-lc");
            ListSelectedOption.AddAlias("-ls");
            
            SearchOption.AddAlias("-s");
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
            Root.Add(CbzOption);
            Root.Add(SlowOption);
            Root.Add(OnlyTranslatorOption);
            Root.Add(PreferTranslatorOption);
            Root.Add(ListChaptersOption);
            Root.Add(ListSelectedOption);
            Root.Add(SearchOption);

            Root.AddArgument(URLArg);

            Root.SetHandler(handler.InvokeAsync);

            return Root;
        }
    }
}