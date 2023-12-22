using Spectre.Console;

namespace MangaInUaDownloader.Downloaders
{
    public abstract class DownloadTask
    {
        protected List<string> Links = null!;
        protected string Location = null!;
        protected float Chapter;
        protected bool Chapterize;

        public DownloadTask Of(List<string> links, string path, float chapter, bool chapterize)
        {
            Links = links;
            Location = path;
            Chapter = chapter;
            Chapterize = chapterize;

            return this;
        }

        public abstract Task Run(ProgressTask progress);
    }
}