namespace MangaInUaDownloader.Downloaders
{
    public abstract class DownloadTask
    {
        protected readonly List<string> Links;
        protected string Location;
        protected readonly float Chapter;
        protected readonly bool Chapterize;

        protected DownloadTask(List<string> links, string path, float chapter, bool chapterize)
        {
            Links = links;
            Location = path;
            Chapter = chapter;
            Chapterize = chapterize;
        }

        public abstract Task Run();
    }
}