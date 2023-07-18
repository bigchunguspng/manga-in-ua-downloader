namespace MangaInUaDownloader.Downloaders
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
}