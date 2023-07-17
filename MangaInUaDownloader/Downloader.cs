using System.Net;
using MangaInUaDownloader.Model;
using MangaInUaDownloader.Services;

#pragma warning disable SYSLIB0014

namespace MangaInUaDownloader
{
    public interface MangaDownloader
    {
        public Task DownloadChapters(IEnumerable<MangaChapter> chapters);
        public Task DownloadChapter(MangaChapter chapter, string path);
    }
    
    public class ImageDownloader : MangaDownloader
    {

        private readonly bool _chapterize, _directory;
        private readonly MangaService _mangaService;
        private int page_number;
        private string? chapter_number;

        public ImageDownloader(bool chapterize) => _chapterize = chapterize;

        public ImageDownloader(bool chapterize, bool directory, MangaService mangaService)
        {
            _chapterize = chapterize;
            _directory = directory;
            _mangaService = mangaService;
        }

        public async Task DownloadChapters(IEnumerable<MangaChapter> chapters)
        {
            var volumes = chapters.GroupBy(x => x.Volume).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var volume in volumes)
            {
                ResetPageCount();

                var volumePath = CreateVolumePath(volume.Key);
                foreach (var chapter in volume.Value)
                {
                    if (_chapterize)
                    {
                        await DownloadChapter(chapter, CreateChapterPath(chapter, volumePath));
                        
                        ResetPageCount();
                    }
                    else
                    {
                        chapter_number = $"{chapter.Chapter}";
                        await DownloadChapter(chapter, volumePath);
                    }
                }
            }
        }

        private string CreateVolumePath(int i) => Directory.CreateDirectory($"Том {i}").Name;

        private string CreateChapterPath(MangaChapter chapter, string volumePath)
        {
            var title = chapter.Title == MangaService.UNTITLED
                ? $"Розділ {chapter.Chapter}"
                : $"Розділ {chapter.Chapter} - {chapter.Title}";
            var chapterPath = Path.Combine(volumePath, title);
            Directory.CreateDirectory(chapterPath);
            return chapterPath;
        }

        private void ResetPageCount() => page_number = 1;
        
        public async Task DownloadChapter(MangaChapter chapter, string path)
        {
            var links = await _mangaService.GetChapterPages(chapter.URL);

            DownloadPages(links, path);
        }

        private void DownloadPages(List<string> links, string path)
        {
            using var client = new WebClient();
            foreach (var link in links)
            {
                var number = page_number++.ToString().PadLeft(_chapterize ? 2 : 3, '0');
                var prefix = _chapterize ? "" : $"{chapter_number} - ";
                var output = Path.Combine(path, $"{prefix}{number}{Path.GetExtension(link)}");
                client.DownloadFile(link, output);
                Console.WriteLine($"[downloaded] \"{output}\"");
            }
        }
    }
}