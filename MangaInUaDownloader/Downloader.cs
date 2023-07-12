using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using PuppeteerSharp;
using PuppeteerSharp.Input;

#pragma warning disable SYSLIB0014

namespace MangaInUaDownloader
{
    public class Downloader
    {
        private const string miu = "https://manga.in.ua";

        private static readonly Regex _ul = new(@"<ul class=""xfieldimagegallery.*ul>");
        private static readonly Regex _li = new(@"<li.*?src=""(.*?)"".*?li>");
        private static readonly Regex _chapter_url  = new(@"https?:\/\/manga\.in\.ua\/chapters\/\S+");

        private readonly bool _chapterize;

        public Downloader(bool chapterize) => _chapterize = chapterize;

        public async Task DownloadChapters(IEnumerable<MangaChapter> chapters)
        {
            var volumes = chapters.GroupBy(x => x.Volume);
            foreach (var volume in volumes)
            {
                var volumeDir = Directory.CreateDirectory($"Том {volume.Key}").Name;
                foreach (var chapter in volume)
                {
                    if (_chapterize)
                    {
                        var chapterDir = chapter.Title == MangaService.UNTITLED
                            ? $"Розділ {chapter.Chapter}"
                            : $"Розділ {chapter.Chapter} - {chapter.Title}";
                        Directory.CreateDirectory(chapterDir);
                        await DownloadChapter(chapter, Path.Combine(volumeDir, chapterDir));
                    }
                    else
                    {
                        await DownloadChapter(chapter, volumeDir);
                    }
                }
            }
        }

        private async Task DownloadChapter(MangaChapter chapter, string path)
        {
            var html = await GetFullHTML(chapter.URL);
            var pages = GetAllPages(html);

            var links = pages.Select(node => node.Attributes["data-src"].Value).ToList();

            using var client = new WebClient();
            for (var i = 0; i < links.Count; i++)
            {
                var number = (i + 1).ToString().PadLeft(_chapterize ? 2 : 3, '0');
                var output = Path.Combine(path, $"{number}{Path.GetExtension(links[i])}");
                client.DownloadFile(links[i], output);
                Console.WriteLine($"[downloaded] \"{output}\"");
            }
        }
        
        private async Task<string> GetFullHTML(string url)
        {
            Console.WriteLine("Fetching browser...");
            using var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
            Console.WriteLine("Launching Puppeteer...");
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });

            Console.WriteLine("Opening browser...");
            await using var page = await browser.NewPageAsync();
            Console.WriteLine("Opening chapter page...");
            await page.GoToAsync(url);
            await page.WaitForSelectorAsync("div#startloadingcomicsbuttom");
            Console.WriteLine("Clicking...");
            await page.ClickAsync("div#startloadingcomicsbuttom");
            Console.WriteLine("Waiting for pages...");
            await page.WaitForSelectorAsync("div#comics ul.xfieldimagegallery.loadcomicsimages");
            return await page.GetContentAsync();
        }
        
        private HtmlNodeCollection GetAllPages(string html)
        {
            Console.WriteLine("Collecting pages...");
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return doc.DocumentNode.SelectNodes("//div[@id='comics']//ul[@class='xfieldimagegallery loadcomicsimages']//li//img");
        }
    }
}