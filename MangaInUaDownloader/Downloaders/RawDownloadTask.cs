using MangaInUaDownloader.Utils.ConsoleExtensions;
using Spectre.Console;

namespace MangaInUaDownloader.Downloaders
{
    /// <summary> Downloads all chapter's pages to a specified location in a raw format. </summary>
    public class RawDownloadTask : DownloadTask
    {
        private static readonly HttpClient _client = new();

        private int page;

        public override async Task Run(ProgressTask progress)
        {
            progress.MaxValue = Links.Count;
            progress.StartTask();
            progress.SetStatus("[olive]Downloading...[/]");
            Location = Directory.CreateDirectory(Location).FullName;

            foreach (var link in Links)
            {
                var number = Chapterize ? PageNumber() : ChapterPageNumber();
                var output = Path.Combine(RelativePath(), $"{number}{Path.GetExtension(link)}");

                await using var stream = await _client.GetStreamAsync(link);
                await using var fs = new FileStream(output, FileMode.Create);
                await stream.CopyToAsync(fs);

                progress.Increment(1);
            }
            progress.SetStatus("[green]Done ✓[/]");
        }

        private string RelativePath() => Path.GetRelativePath(Environment.CurrentDirectory, Location);

        private string ChapterPageNumber() => $"{Chapter} - {(++page).ToString().PadLeft(3, '0')}";
        private string        PageNumber() =>                (++page).ToString().PadLeft(2, '0');
    }
}