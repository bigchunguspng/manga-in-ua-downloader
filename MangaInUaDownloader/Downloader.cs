using System.Net;
using System.Text.RegularExpressions;
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
        private const string XPATH_PAGES = "//div[@id='comics']//ul[@class='xfieldimagegallery loadcomicsimages']//li//img";
        private const string SELECTOR_BUTTON = "div#startloadingcomicsbuttom a";
        private const string SELECTOR_UL = "div#comics ul.xfieldimagegallery.loadcomicsimages";

        private readonly Regex _title = new(@"Том: (.+)\. Розділ: (.+?) .+");

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
            var html = await GetFullHTML(chapter.URL);
            var pages = ScrapService.Instance.GetHTMLNodes(html, XPATH_PAGES, "Collecting pages...");

            if (chapter.Volume < 0)
            {
                // get name and shit
                var title = ScrapService.Instance.GetHTMLNode(html, "//head//title").InnerText;
                var match = _title.Match(title);
                chapter.Volume = Convert.ToInt32(match.Groups[1].Value);
                chapter.Chapter = Convert.ToSingle(match.Groups[2].Value);
                var v = CreateVolumePath(chapter.Volume);
                path = _chapterize ? CreateChapterPath(chapter, v) : v;
                chapter_number = $"{chapter.Chapter}";
            }

            var links = pages.Select(node => node.Attributes["data-src"].Value).ToList();

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

        private async Task<string> GetFullHTML(string url)
        {
            var page = await ScrapService.Instance.OpenWebPageAsync(url, "chapter");
            
            await ScrapService.Instance.Click(page, SELECTOR_BUTTON);
            await ScrapService.Instance.LoadElement(page, SELECTOR_UL, "pages");
            
            return await ScrapService.Instance.GetContent(page);
        }
    }
}