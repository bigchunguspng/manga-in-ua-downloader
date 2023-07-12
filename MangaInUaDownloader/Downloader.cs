using System.Net;
using System.Text.RegularExpressions;

#pragma warning disable SYSLIB0014

namespace MangaInUaDownloader
{
    public class Downloader
    {
        private const string miu = "https://manga.in.ua";

        private const string XPATH_PAGES = "//div[@id='comics']//ul[@class='xfieldimagegallery loadcomicsimages']//li//img";
        private const string SELECTOR_BUTTON = "div#startloadingcomicsbuttom a";
        private const string SELECTOR_UL = "div#comics ul.xfieldimagegallery.loadcomicsimages";

        private static readonly Regex _chapter_url  = new(@"https?:\/\/manga\.in\.ua\/chapters\/\S+");

        private readonly bool _chapterize;
        private int page_number;

        public Downloader(bool chapterize) => _chapterize = chapterize;
        

        public async Task DownloadChapters(IEnumerable<MangaChapter> chapters)
        {
            var volumes = chapters.GroupBy(x => x.Volume).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var volume in volumes)
            {
                ResetPageCount();
                
                var volumeDir = Directory.CreateDirectory($"Том {volume.Key}").Name;
                foreach (var chapter in volume.Value)
                {
                    if (_chapterize)
                    {
                        var title = chapter.Title == MangaService.UNTITLED
                            ? $"Розділ {chapter.Chapter}"
                            : $"Розділ {chapter.Chapter} - {chapter.Title}";
                        var chapterDir = Path.Combine(volumeDir, title);
                        Directory.CreateDirectory(chapterDir);
                        await DownloadChapter(chapter, chapterDir);
                        
                        ResetPageCount();
                    }
                    else
                    {
                        await DownloadChapter(chapter, volumeDir);
                    }
                }
            }
        }

        private void ResetPageCount() => page_number = 1;

        private async Task DownloadChapter(MangaChapter chapter, string path)
        {
            var html = await GetFullHTML(chapter.URL);
            var pages = ScrapService.Instance.GetHTMLNodes(html, XPATH_PAGES, "Collecting pages...");

            var links = pages.Select(node => node.Attributes["data-src"].Value).ToList();

            using var client = new WebClient();
            foreach (var link in links)
            {
                var number = page_number++.ToString().PadLeft(_chapterize ? 2 : 3, '0');
                var output = Path.Combine(path, $"{number}{Path.GetExtension(link)}");
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