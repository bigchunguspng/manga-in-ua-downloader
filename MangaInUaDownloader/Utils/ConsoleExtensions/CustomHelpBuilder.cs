using System.CommandLine;
using System.CommandLine.Help;
using Spectre.Console;

namespace MangaInUaDownloader.Utils.ConsoleExtensions
{
    public class CustomHelpBuilder : HelpBuilder
    {
        private const string Indent = "  ";
        
        public CustomHelpBuilder(Localization localization, int maxWidth = int.MaxValue) : base(localization, maxWidth) { }
        
        public override void Write(HelpContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            if (context.Command.IsHidden) return;

            foreach (var writeSection in GetLayout())
            {
                writeSection(context);
                AnsiConsole.WriteLine();
            }
        }

        /// <summary> Gets the default sections to be written for command line help. </summary>
        private IEnumerable<HelpSectionDelegate> GetLayout()
        {
            yield return SynopsisSection();
            yield return CommandUsageSection();
            yield return CommandArgumentsSection();
            yield return OptionsSection();
            yield return SubcommandsSection();
        }

        /// <summary> Writes a help section describing a command's synopsis. </summary>
        private HelpSectionDelegate SynopsisSection() => ctx =>
        {
            WriteHeading(ctx.HelpBuilder.LocalizationResources.HelpDescriptionTitle(), ctx.Command.Description);
        };

        /// <summary> Writes a help section describing a command's usage. </summary>
        private HelpSectionDelegate CommandUsageSection() => ctx =>
        {
            var usage = string.Join(" ", GetUsageParts(ctx.Command).Where(x => !string.IsNullOrWhiteSpace(x)));

            WriteHeading(ctx.HelpBuilder.LocalizationResources.HelpUsageTitle(), usage);

            IEnumerable<string> GetUsageParts(Command command)
            {
                var displayOptionTitle = false;

                var parentCommands = command
                    .RecurseWhileNotNull(c => c.Parents.OfType<Command>().FirstOrDefault())
                    .Reverse();

                foreach (var parentCommand in parentCommands)
                {
                    if (displayOptionTitle == false)
                    {
                        displayOptionTitle = parentCommand.Options.Any(IsGlobalAndNotHidden);
                    }

                    yield return parentCommand.Name;

                    //yield return FormatArgumentUsage(parentCommand.Arguments);
                    yield return $"<{parentCommand.Arguments.First().Name}>";
                }

                var hasCommandWithHelp = command.Subcommands.Any(x => !x.IsHidden);

                if (hasCommandWithHelp)
                {
                    yield return LocalizationResources.HelpUsageCommand();
                }

                displayOptionTitle = displayOptionTitle || command.Options.Any(x => !x.IsHidden);
                
                if (displayOptionTitle)
                {
                    yield return LocalizationResources.HelpUsageOptions();
                }

                if (!command.TreatUnmatchedTokensAsErrors)
                {
                    yield return LocalizationResources.HelpUsageAdditionalArguments();
                }
            }
        };

        ///  <summary> Writes a help section describing a command's arguments.  </summary>
        private HelpSectionDelegate CommandArgumentsSection() => ctx =>
        {
            var commandArguments = GetCommandArgumentRows(ctx.Command, ctx).ToArray();

            if (commandArguments.Length <= 0) return;

            WriteHeading(ctx.HelpBuilder.LocalizationResources.HelpArgumentsTitle());
            WriteColumns(commandArguments);

            IEnumerable<TwoColumnHelpRow> GetCommandArgumentRows(Command command, HelpContext context) => command
                .RecurseWhileNotNull(c => c.Parents.OfType<Command>().FirstOrDefault())
                .Reverse()
                .SelectMany(cmd => cmd.Arguments.Where(a => !a.IsHidden))
                .Select(a => GetTwoColumnRow(a, context))
                .Distinct();
        };


        ///  <summary> Writes a help section describing a command's options.  </summary>
        private HelpSectionDelegate OptionsSection() => ctx =>
        {
            // by making this logic more complex, we were able to get some nice perf wins elsewhere
            List<TwoColumnHelpRow> options = new();
            HashSet<Option> uniqueOptions = new();
            foreach (var option in ctx.Command.Options)
            {
                if (!option.IsHidden && uniqueOptions.Add(option))
                {
                    options.Add(ctx.HelpBuilder.GetTwoColumnRow(option, ctx));
                }
            }

            var current = ctx.Command;
            while (current is not null)
            {
                Command? parentCommand = null;
                var parent = new ParentNode(current.Parents.FirstOrDefault());
                while (parent is not null)
                {
                    if ((parentCommand = parent.Symbol as Command) is not null)
                    {
                        foreach (var option in parentCommand.Options)
                        {
                            // global help aliases may be duplicated, we just ignore them
                            if (IsGlobalAndNotHidden(option) && uniqueOptions.Add(option))
                            {
                                options.Add(ctx.HelpBuilder.GetTwoColumnRow(option, ctx));
                            }
                        }

                        break;
                    }

                    parent = parent.Next;
                }

                current = parentCommand;
            }

            if (options.Count <= 0) return;

            WriteHeading(ctx.HelpBuilder.LocalizationResources.HelpOptionsTitle());
            WriteColumns(options);
            AnsiConsole.WriteLine();
        };

        ///  <summary> Writes a help section describing a command's subcommands.  </summary>
        private HelpSectionDelegate SubcommandsSection() => ctx =>
        {
            var subcommands = ctx.Command.Subcommands.Where(x => !x.IsHidden).Select(x => GetTwoColumnRow(x, ctx)).ToArray();

            if (subcommands.Length <= 0) return;

            WriteHeading(LocalizationResources.HelpCommandsTitle());
            WriteColumns(subcommands);
        };


        private void WriteHeading(string? heading, string? description = null)
        {
            if (!string.IsNullOrWhiteSpace(heading))
            {
                AnsiConsole.MarkupLine(heading);
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                foreach (var part in WrapText(description, MaxWidth - Indent.Length))
                {
                    AnsiConsole.Write(Indent);
                    AnsiConsole.MarkupLine(part.EscapeMarkup());
                }
            }
        }

        private void WriteColumns(IReadOnlyList<TwoColumnHelpRow> items)
        {
            if (items.Count == 0) return;

            var windowWidth = MaxWidth;

            var column1Width = items.Select(x =>  x.FirstColumnText.Length).Max();
            var column2Width = items.Select(x => x.SecondColumnText.Length).Max();

            if (column1Width + column2Width + Indent.Length + Indent.Length > windowWidth)
            {
                var column1MaxWidth = windowWidth / 2 - Indent.Length;
                if (column1Width > column1MaxWidth)
                {
                    column1Width = items.SelectMany(x => WrapText(x.FirstColumnText, column1MaxWidth).Select(s => s.Length)).Max();
                }
                column2Width = windowWidth - column1Width - Indent.Length - Indent.Length;
            }
            
            foreach (var row in items)
            {
                var column1Parts = WrapText(row.FirstColumnText,  column1Width);
                var column2Parts = WrapText(row.SecondColumnText, column2Width);

                foreach (var (first, second) in ZipWithEmpty(column1Parts, column2Parts))
                {
                    AnsiConsole.Markup($"{Indent}{first}");
                    if (!string.IsNullOrWhiteSpace(second))
                    {
                        var padSize = column1Width - first.Length;
                        var padding = padSize > 0 ? new string(' ', padSize) : "";

                        AnsiConsole.MarkupInterpolated($"{padding}{Indent}{second}");
                    }

                    AnsiConsole.WriteLine();
                }
            }

            static IEnumerable<(string, string)> ZipWithEmpty(IEnumerable<string> first, IEnumerable<string> second)
            {
                using var enum1 =  first.GetEnumerator();
                using var enum2 = second.GetEnumerator();
                bool hasFirst, hasSecond;
                while ((hasFirst = enum1.MoveNext()) | (hasSecond = enum2.MoveNext()))
                {
                    yield return (hasFirst ? enum1.Current : "", hasSecond ? enum2.Current : "");
                }
            }
        }


        private bool IsGlobalAndNotHidden(Option x)
        {
            return (bool)(x.GetType().GetProperty("IsGlobal")?.GetValue(x) ?? false) && !x.IsHidden;
        }

        private static IEnumerable<string> WrapText(string text, int maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;

            //First handle existing new lines
            var parts = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var part in parts)
            {
                if (part.Length <= maxWidth) yield return part;
                else
                {
                    //Long item, wrap it based on the width
                    for (var i = 0; i < part.Length;)
                    {
                        if (part.Length - i < maxWidth)
                        {
                            yield return part.Substring(i);
                            break;
                        }
                        else
                        {
                            var length = -1;
                            for (var j = 0; j + i < part.Length && j < maxWidth; j++)
                            {
                                if (char.IsWhiteSpace(part[i + j]))
                                {
                                    length = j + 1;
                                }
                            }
                            if (length == -1)
                            {
                                length = maxWidth;
                            }
                            yield return part.Substring(i, length);

                            i += length;
                        }
                    }
                }
            }
        }
    }
    
    internal sealed class ParentNode
    {
        internal ParentNode(Symbol? symbol) => Symbol = symbol;

        internal Symbol? Symbol { get; }

        internal ParentNode? Next { get; set; }
    }

    public class Localization : LocalizationResources
    {
        /// <summary> Gets a global instance of the <see cref="Localization"/> class. </summary>
        public new static Localization Instance { get; } = new();
    }
}