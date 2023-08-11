using System.CommandLine;
using System.CommandLine.Parsing;
using Spectre.Console;

namespace MangaInUaDownloader.Utils.ConsoleExtensions
{
    public class Localization : LocalizationResources
    {
        /// <summary> Gets a global instance of the <see cref="Localization"/> class. </summary>
        public new static Localization Instance { get; } = new();

        public override string HelpUsageTitle() => "[gold1]Використання:[/]";

        public override string HelpArgumentsTitle() => "[gold1]Аргументи:[/]";

        public override string HelpOptionsTitle() => "[gold1]Опції:[/]";

        public override string HelpCommandsTitle() => "[gold1]Команди:[/]";

        public override string HelpUsageOptions() => Markup.Escape("[опції]");

        public override string VersionOptionDescription() => "Версія програми.";

        public override string HelpOptionDescription() => "Довідка.";

        public override string RequiredArgumentMissing(SymbolResult symbolResult)
        {
            return $"Команда \"{symbolResult.Symbol.Name}\" була викликана без необхідного аргументу.";
        }

        public override string ArgumentConversionCannotParseForOption(string value, string option, Type expected)
        {
            return $"Не вдалося розібрати аргумент \"{value}\" до опції \"{option}\" як значення типу \"{expected}\".";
        }

        public override string UnrecognizedCommandOrArgument(string arg)
        {
            return $"Не вдалося розпізнати аргумент або команду \"{arg}\".";
        }

        public override string SuggestionsTokenNotMatched(string token)
        {
            return $"Що ще за \"{token}\"? Можливо ви мали на увазі щось із цього?";
        }

        public override string VersionOptionCannotBeCombinedWithOtherArguments(string option)
        {
            return $"Опцію \"{option}\" не можна поєднувати з іншими аргументами.";
        }
    }
}