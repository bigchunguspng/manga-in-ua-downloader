using MangaInUaDownloader.Model;

namespace MangaInUaDownloader.Services
{
    public interface MangaService
    {
        public const string UNTITLED = "(Без назви)";
        
        public bool IsChapterURL(string url);
        public bool   IsMangaURL(string url);

        /// <summary> Returns chapters grouped by their title </summary>
        public Task<Dictionary<string, List<MangaChapter>>> GetChaptersGrouped(string url);
        
        /// <summary> Returns chapters selected by provided options </summary>
        public Task<IEnumerable<MangaChapter>> GetChapters(string url, MangaDownloadOptions options);
        
        /// <summary> Returns a list of every chapter's page URL </summary>
        public Task<List<string>> GetChapterPages(string url);
        
        public Task<string> GetMangaTitle(string url);

        public Task<MangaChapter> GetChapterDetails(string url);
    }
}