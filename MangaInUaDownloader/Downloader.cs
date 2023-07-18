using System.Net;

#pragma warning disable SYSLIB0014

namespace MangaInUaDownloader
{
    public abstract class DownloadTask
    {
        protected readonly List<string> Links;
        protected readonly string Location;
        protected readonly float Chapter;

        protected DownloadTask(List<string> links, string path, float chapter)
        {
            Links = links;
            Location = path;
            Chapter = chapter;
        }

        public abstract Task Run();
    }

    /// <summary> Downloads all chapter's pages to a specified location. </summary>
    public class RawDownloadTask : DownloadTask
    {
        private int page;

        private readonly bool _chapterize;

        public RawDownloadTask(List<string> links, string path, float chapter, bool chapterize) : base(links, path, chapter)
        {
            _chapterize = chapterize;
        }
        
        public override async Task Run()
        {
            Directory.CreateDirectory(Location);
            using var client = new WebClient();
            foreach (var link in Links)
            {
                var number = page++.ToString().PadLeft(_chapterize ? 2 : 3, '0');
                var prefix = _chapterize ? "" : $"{Chapter} - ";
                var output = Path.Combine(Location, $"{prefix}{number}{Path.GetExtension(link)}");
                await client.DownloadFileTaskAsync(link, output);
                Console.WriteLine($"[downloaded] \"{output}\"");
            }
        }
    }
}