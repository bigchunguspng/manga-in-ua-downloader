using MangaInUaDownloader.Model;

namespace MangaInUaDownloader.Services
{
    public interface MangaService
    {
        public  const string UNTITLED = "(Без назви)";
        
        public bool IsChapterURL(string url);
        public bool   IsMangaURL(string url);

        public Task<Dictionary<string, List<MangaChapter>>> GetTranslatorsByChapter(string url);
        public Task<IEnumerable<MangaChapter>> GetChapters(string url, MangaDownloadOptions options);
    }
}