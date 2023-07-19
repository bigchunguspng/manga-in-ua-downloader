using MangaInUaDownloader.Model;
using MangaInUaDownloader.Utils.ConsoleExtensions;

namespace MangaInUaDownloader.Services
{
    public interface MangaService
    {
        public const string UNTITLED = "(Без назви)";
        
        public bool IsChapterURL(string url);
        public bool   IsMangaURL(string url);

        /// <summary>
        /// Returns chapters grouped by their title
        /// </summary>
        public Task<Dictionary<string, List<MangaChapter>>> GetChaptersGrouped(string url, IStatus status);
        
        /// <summary>
        /// Returns chapters selected by provided options
        /// </summary>
        public Task<IEnumerable<MangaChapter>> GetChapters(string url, MangaDownloadOptions options, IStatus status);
        
        /// <summary>
        /// Returns URL of every chapter's page
        /// </summary>
        public Task<List<string>> GetChapterPages   (string url, IStatus status);

        public Task<MangaChapter> GetChapterDetails (string url, IStatus status);
        
        public Task<string>       GetMangaTitle     (string url, IStatus status);
    }
}