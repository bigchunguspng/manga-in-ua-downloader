using System.CommandLine;
using System.CommandLine.Invocation;

namespace MangaInUaDownloader.Commands
{
    public static class RootCommandBuilder
    {
        private const string _chapter = "розділ", _volume = "том", _nick = "нік", _name = "назва";

        private static readonly Command Root = new("MiUD", "[bold]Ця програма[/] завантажує [yellow]манґу[/] з сайту [deeppink3]https://manga.in.ua[/]");

        public static readonly Option<float>     ChapterOption = new(     "--chapter", () => float.MinValue, "Розділ, що слід завантажити.") { ArgumentHelpName = _chapter };
        public static readonly Option<float> FromChapterOption = new("--from-chapter", () => float.MinValue, "Перший розділ, що слід завантажити.") { ArgumentHelpName = _chapter };
        public static readonly Option<float>   ToChapterOption = new(  "--to-chapter", () => float.MaxValue, "Останній розділ, що слід завантажити.\n") { ArgumentHelpName = _chapter };

        public static readonly Option<int>     VolumeOption = new(     "--volume", () => int.MinValue, "Том, розділи якого слід завантажити.") { ArgumentHelpName = _volume };
        public static readonly Option<int> FromVolumeOption = new("--from-volume", () => int.MinValue, "Перший том, що слід завантажити.") { ArgumentHelpName = _volume };
        public static readonly Option<int>   ToVolumeOption = new(  "--to-volume", () => int.MaxValue, "Останній том, що слід завантажити.\n") { ArgumentHelpName = _volume };

        public static readonly Option<string>    TitleOption = new("--title",      "Зберігає тайтл під іншою назвою.") { ArgumentHelpName = _name };
        public static readonly Option<bool>  DirectoryOption = new("--directory",  "Завантажує томи манґи до поточної директорії.");
        public static readonly Option<bool> ChapterizeOption = new("--chapterize", "Зберігає вміст кожного розділу до окремої теки.\n");

        public static readonly Option<bool>        CbzOption = new("--cbz",  "Зберігає манґу у форматі \".cbz\".");
        public static readonly Option<bool>       SlowOption = new("--slow", "Завантажує розділи один за одним [dim](повільніше)[/]\n");

        public static readonly Option<string>   OnlyTranslatorOption = new("--only-translator", "Обирає лише розділи з певним перекладом.") { ArgumentHelpName = _nick };
        public static readonly Option<string> PreferTranslatorOption = new("--prefer-translator", "Надає перевагу розділам з певним перекладом.\n") { ArgumentHelpName = _nick };

        public static readonly Option<bool> ListChaptersOption = new("--list-chapters", "Перелічує всі розділи, що є на сайті. [dim](без завантаження)[/]");
        public static readonly Option<bool> ListSelectedOption = new("--list-selected", "Перелічує всі розділи, що відповідають запиту. [dim](без завантаження)[/]\n");

        public static readonly Option<bool> SearchOption = new("--search", "Здійснює пошук манґи. [dim](URL не потрібен)[/]\n") { ArgumentHelpName = "пошуковий запит" };

        public static readonly Argument<Uri> URLArg = new("URL", "Посилання на [yellow]сторінку манґи чи її розділ[/], на зразок цього: [deeppink3]https://manga.in.ua/….html.[/]");

        public static Command Build(ICommandHandler handler)
        {
            AddOption("-c",  ChapterOption);
            AddOption("-fc", FromChapterOption);
            AddOption("-tc", ToChapterOption);
            AddOption("-v",  VolumeOption);
            AddOption("-fv", FromVolumeOption);
            AddOption("-tv", ToVolumeOption);
            AddOption("-t",  TitleOption);
            AddOption("-d",  DirectoryOption);
            AddOption("-cp", ChapterizeOption);
            AddOption("-z",  CbzOption);
            AddOption("-w",  SlowOption);
            AddOption("-o",  OnlyTranslatorOption);
            AddOption("-p",  PreferTranslatorOption);
            AddOption("-lc", ListChaptersOption);
            AddOption("-ls", ListSelectedOption);
            AddOption("-s",  SearchOption);

            Root.AddArgument(URLArg);

            Root.SetHandler(handler.InvokeAsync);

            return Root;
        }

        private static void AddOption(string alias, Option option)
        {
            option.AddAlias(alias);
            Root.Add(option);
        }
    }
}