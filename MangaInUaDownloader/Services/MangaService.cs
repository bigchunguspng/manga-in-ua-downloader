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
        /// Returns every translation of each chapter. Chapters are distinct by their volume and chapter numbers.
        /// </summary>
        public Task<List<List<MangaChapter>>> GetTranslations(string url, IStatus status);
        
        /// <summary>
        /// Returns a collection of manga chapters selected by provided options.
        /// </summary>
        public Task<IEnumerable<MangaChapter>> GetChapters(string url, IStatus status, MangaDownloadOptions options);
        
        /// <summary>
        /// Returns a URL of every page of a chapter by chapter's URL.
        /// </summary>
        public Task<List<string>> GetChapterPages   (string url, IStatus status);

        /// <summary>
        /// Returns a <see cref="MangaChapter"/> object with fully or partially initialized data by chapter's URL.
        /// </summary>
        public Task<MangaChapter> GetChapterDetails (string url, IStatus status);
        
        /// <summary>
        /// Returns a string representation of manga's title by chapter or manga URL.
        /// </summary>
        public Task<string>       GetMangaTitle     (string url, IStatus status);
    }
}