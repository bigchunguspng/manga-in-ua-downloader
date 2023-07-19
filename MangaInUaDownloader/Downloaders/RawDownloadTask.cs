using System.Net;
using MangaInUaDownloader.Utils.ConsoleExtensions;
using Spectre.Console;

#pragma warning disable SYSLIB0014

namespace MangaInUaDownloader.Downloaders
{
    /// <summary> Downloads all chapter's pages to a specified location in a raw format. </summary>
    public class RawDownloadTask : DownloadTask
    {
        private int page;

        public RawDownloadTask(List<string> links, string path, float chapter, bool chapterize) : base(links, path, chapter, chapterize) { }
        
        public override async Task Run(ProgressTask progress)
        {
            progress.MaxValue = Links.Count;
            progress.StartTask();
            progress.SetStatus("[olive]Downloading...[/]");
            Location = Directory.CreateDirectory(Location).FullName;
            
            using var client = new WebClient();
            foreach (var link in Links)
            {
                var number = Chapterize ? PageNumber() : ChapterPageNumber();
                var output = Path.Combine(RelativePath(), $"{number}{Path.GetExtension(link)}");
                await client.DownloadFileTaskAsync(link, output);
                progress.Increment(1);
                //Console.WriteLine($"[downloaded] \"{output}\"");
            }
            progress.SetStatus("[green]Done ✓[/]");
        }

        private string RelativePath() => Path.GetRelativePath(Environment.CurrentDirectory, Location);

        private string ChapterPageNumber() => $"{Chapter} - {(++page).ToString().PadLeft(3, '0')}";
        private string        PageNumber() =>                (++page).ToString().PadLeft(2, '0');
    }
}