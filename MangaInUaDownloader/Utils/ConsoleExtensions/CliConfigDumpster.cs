using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Reflection;
using MangaInUaDownloader.Commands;
using Spectre.Console;
using System.Text.Json;

namespace MangaInUaDownloader.Utils.ConsoleExtensions;

public static class CliConfigDumpster
{
    public static readonly HelpBuilder HelpBuilder;

    static CliConfigDumpster()
    {
        HelpBuilder = new CustomHelpBuilder(Localization.Instance, Console.WindowWidth);
        HelpBuilder.HideDefaultValue(RootCommandBuilder.ChapterOption);
        HelpBuilder.HideDefaultValue(RootCommandBuilder.FromChapterOption);
        HelpBuilder.HideDefaultValue(RootCommandBuilder.ToChapterOption);
        HelpBuilder.HideDefaultValue(RootCommandBuilder.VolumeOption);
        HelpBuilder.HideDefaultValue(RootCommandBuilder.FromVolumeOption);
        HelpBuilder.HideDefaultValue(RootCommandBuilder.ToVolumeOption);
    }

    private static void HideDefaultValue(this HelpBuilder builder, Option option)
    {
        builder.CustomizeSymbol(option, secondColumnText: option.Description);
    }


    // MIDDLEWARES

    public static CommandLineBuilder UseParseErrorReporting(this CommandLineBuilder builder)
    {
        builder.AddMiddleware(async (context, next) =>
        {
            if (context.ParseResult.Errors.Count > 0)
                context.InvocationResult = new ParseErrorResult(HelpBuilder);
            else
                await next(context);
        }, MiddlewareOrder.ErrorReporting);

        return builder;
    }

    public static CommandLineBuilder UseVersion(this CommandLineBuilder builder, params string[] aliases)
    {
        var versionOption = new VersionOption(aliases);

        builder.Command.AddOption(versionOption);
        builder.AddMiddleware(async (context, next) =>
        {
            if (context.ParseResult.FindResultFor(versionOption) is not null)
            {
                if (context.ParseResult.Errors.Any(e => e.SymbolResult?.Symbol is VersionOption))
                    context.InvocationResult = new ParseErrorResult(HelpBuilder);
                else
                {
                    var current = AssemblyVersion.Value;
                    AnsiConsole.MarkupLine($"\n[yellow]Поточна версія:[/] {current}");
                    try
                    {
                        var latest = await GetLatestVersion();
                        if (latest is null) return;

                        var x = CompareVersions(current.Split('+')[0], latest.tag_name);
                        var hint = x > 0 ? "(новіша)" : x < 0 ? "(старіша)" : "";
                        AnsiConsole.MarkupLine($"[yellow]Остання версія:[/] {latest.tag_name} [dim]{hint}[/]\n");
                        AnsiConsole.MarkupLine("[yellow]Завантажити:[/]");
                        AnsiConsole.MarkupLine("[deeppink3]https://github.com/bigchunguspng/manga-in-ua-downloader/releases[/]\n");
                    }
                    catch
                    {
                        // YOHOHO!
                    }
                }
            }
            else
                await next(context);
        });

        return builder;
    }


    // FETCH VERSIONS

    private record LatestReleaseInfo(string tag_name);

    private const string LATEST = "https://api.github.com/repos/bigchunguspng/manga-in-ua-downloader/releases/latest";

    private static async Task<LatestReleaseInfo?> GetLatestVersion()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "request");
        var response = await client.GetAsync(LATEST);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LatestReleaseInfo>(content);
    }

    public static readonly Lazy<string> AssemblyVersion = new(() =>
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return versionAttribute is null
            ? assembly.GetName().Version?.ToString().TrimEnd('0', '.') ?? "" // 2.5.4.0 -> 2.5.4
            : versionAttribute.InformationalVersion;                         // 2.5.4+f582fa6fe…
    });

    private static int CompareVersions(string current, string latest)
    {
        var parts1 = current.Split('.');
        var parts2 = latest .Split('.');

        var maxLength = Math.Max(parts1.Length, parts2.Length);

        for (var i = 0; i < maxLength; i++)
        {
            var vC = i < parts1.Length ? int.Parse(parts1[i]) : 0;
            var vL = i < parts2.Length ? int.Parse(parts2[i]) : 0;

            if (vC > vL) return -1;
            if (vC < vL) return 1;
        }

        return 0;
    }
}