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
        private int page_number;

        void ResetPageCount() => page_number = 1;

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

        private async Task DownloadChapter(MangaChapter chapter, string path)
        {
            var html = await GetFullHTML(chapter.URL);
            var pages = GetAllPages(html);

            var links = pages.Select(node => node.Attributes["data-src"].Value).ToList();

            using var client = new WebClient();
            for (var i = 0; i < links.Count; i++)
            {
                var number = page_number++.ToString().PadLeft(_chapterize ? 2 : 3, '0');
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
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = false });

            Console.WriteLine("Opening browser...");
            await using var page = await browser.NewPageAsync();
            Console.WriteLine("Opening chapter page...");
            await page.GoToAsync(url);
            await page.WaitForSelectorAsync("div#startloadingcomicsbuttom a", new WaitForSelectorOptions() { Visible = true });
            Console.WriteLine("Clicking...");
            //await Task.Delay(420);
            await page.ClickAsync("div#startloadingcomicsbuttom a", new ClickOptions() { Delay = 95 });
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