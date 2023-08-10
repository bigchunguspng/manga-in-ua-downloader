using System.CommandLine;
using System.CommandLine.Help;
using Spectre.Console;

namespace MangaInUaDownloader.Utils.ConsoleExtensions
{
    public class CustomHelpBuilder : HelpBuilder
    {
        private const string Indent = "  ";
        
        public CustomHelpBuilder(LocalizationResources localization, int maxWidth = int.MaxValue) : base(localization, maxWidth) { }
        
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
            WriteHeading(Indent, ctx.Command.Description);
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

                    yield return FormatArgumentUsage(parentCommand.Arguments);
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
                    options.Add(GetTwoColumnRow(option, ctx));
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
                                options.Add(GetTwoColumnRow(option, ctx));
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
            if (!string.IsNullOrEmpty(heading))
            {
                AnsiConsole.MarkupLine(heading);
            }

            if (!string.IsNullOrEmpty(description))
            {
                foreach (var part in WrapText(description, MaxWidth - Indent.Length))
                {
                    AnsiConsole.Write(Indent);
                    AnsiConsole.MarkupLine(part);
                }
            }
        }

        private void WriteColumns(IReadOnlyList<TwoColumnHelpRow> items)
        {
            if (items.Count == 0) return;

            var windowWidth = MaxWidth;

            var column1Width = items.Select(x => x. FirstColumnText.RemoveMarkup().Length).Max();
            var column2Width = items.Select(x => x.SecondColumnText.RemoveMarkup().Length).Max();

            if (column1Width + column2Width + Indent.Length + Indent.Length > windowWidth)
            {
                var column1MaxWidth = windowWidth / 2 - Indent.Length;
                if (column1Width > column1MaxWidth)
                {
                    column1Width = items.SelectMany(x => WrapText(x.FirstColumnText, column1MaxWidth).Select(s => s.RemoveMarkup().Length)).Max();
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
                        var padSize = column1Width - first.RemoveMarkup().Length;
                        var padding = padSize > 0 ? new string(' ', padSize) : "";

                        AnsiConsole.Markup($"{padding}{Indent}{second}");
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

        private new TwoColumnHelpRow GetTwoColumnRow(Symbol symbol, HelpContext context)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            if (symbol is IdentifierSymbol option) // or command
            {
                return GetOptionRow();
            }
            if (symbol is Argument argument)
            {
                return GetArgumentRow();
            }
            else
                throw new NotSupportedException($"Symbol type {symbol.GetType()} is not supported.");


            TwoColumnHelpRow GetOptionRow()
            {
                var column1 =         GetIdentifierSymbolUsageLabel (option, context);
                var column2 = Default.GetIdentifierSymbolDescription(option);

                return new TwoColumnHelpRow(column1, column2);
            }

            TwoColumnHelpRow GetArgumentRow()
            {
                var column1 =         GetArgumentUsageLabel (argument);
                var column2 = Default.GetArgumentDescription(argument);

                return new TwoColumnHelpRow(column1, column2);
            }
        }


        private static string GetIdentifierSymbolUsageLabel(IdentifierSymbol symbol, HelpContext context)
        {
            var aliases = symbol.Aliases; // todo
            /*.Select(r => r.SplitPrefix())
            .OrderBy(r => r.Prefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Alias, StringComparer.OrdinalIgnoreCase)
            .GroupBy(t => t.Alias)
            .Select(t => t.First())
            .Select(t => $"{t.Prefix}{t.Alias}");*/

            var column1 = string.Join(" / ", aliases);

            var argument = symbol.Argument();
            if (argument is { IsHidden: false })
            {
                var argumentLabel = GetArgumentUsageLabel(argument, optional: true);

                if (!string.IsNullOrWhiteSpace(argumentLabel))
                {
                    column1 += $" {argumentLabel}";
                }
            }

            if (symbol is Option { IsRequired: true })
            {
                column1 += $" {context.HelpBuilder.LocalizationResources.HelpOptionsRequiredLabel()}";
            }

            return column1;
        }

        private static string GetArgumentUsageLabel(Argument argument, bool optional = false)
        {
            var empty = string.IsNullOrEmpty(argument.HelpName);
            if   (optional && empty) return string.Empty;
            return $"[tan][[{(empty ? argument.Name : argument.HelpName)}]][/]";
        }

        private string FormatArgumentUsage(IReadOnlyList<Argument> arguments)
        {
            return $"[tan][[{string.Join("]] [[", arguments.Select(a => a.Name))}]][/]";
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
                var raw = part.RemoveMarkup();
                if (raw.Length <= maxWidth) yield return part;
                else
                {
                    //Long item, wrap it based on the width
                    var segments = new Markup(part).GetSegments(AnsiConsole.Console).ToList();
                    for (var i = 0; i < raw.Length;)
                    {
                        if (raw.Length - i < maxWidth)
                        {
                            yield return RenderMarkup(raw.Substring(i));
                            break;
                        }
                        else
                        {
                            var length = -1;
                            for (var j = 0; j + i < raw.Length && j < maxWidth; j++)
                            {
                                if (char.IsWhiteSpace(raw[i + j]))
                                {
                                    length = j + 1;
                                }
                            }
                            if (length == -1)
                            {
                                length = maxWidth;
                            }
                            yield return RenderMarkup(raw.Substring(i, length));

                            i += length;
                        }
                        
                        string RenderMarkup(string substring) // i = start of substring
                        {
                            List<(int start, int length, Style style)> list = new();
                            var position = 0;
                            for (var n = 0; n < segments.Count && position < i + substring.Length; n++)
                            {
                                var segment = segments[n];
                                // if segment has style and overlaps substring
                                if (!segment.Style.Equals(Style.Plain) && (position >= i || position + segment.Text.Length >= i))
                                {
                                    var index  = position - i;
                                    var start  = Math.Max(index, 0);
                                    var finish = Math.Min(index + segment.Text.Length, substring.Length);
                                    var length = finish - start;
                                    list.Add((start, length, segment.Style));
                                }
                                position += segment.Text.Length;
                            }

                            for (var x = list.Count - 1; x >= 0; x--)
                            {
                                var s = list[x];
                                var a = substring.Substring(0, s.start);
                                var b = substring.Substring(s.start, s.length).EscapeMarkup();
                                var c = substring.Substring(s.start + s.length);
                                substring = $"{a}[{s.style.ToMarkup()}]{b}[/]{c}";
                            }

                            return substring;
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

        public override string HelpUsageTitle() => "[gold1]Використання:[/]";

        public override string HelpArgumentsTitle() => "[gold1]Аргументи:[/]";

        public override string HelpOptionsTitle() => "[gold1]Опції:[/]";

        public override string HelpCommandsTitle() => "[gold1]Команди:[/]";

        public override string HelpUsageOptions() => Markup.Escape("[опції]");
    }
}