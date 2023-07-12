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

        private readonly string _path;

        public Downloader(string path) => _path = Path.Combine(path.Split('\\', '/'));

        public async Task DownloadChapters(IEnumerable<MangaChapter> chapters, bool chapterize)
        {
            var volumes = chapters.GroupBy(x => x.Volume);
            foreach (var volume in volumes)
            {
                var volumeDir = Directory.CreateDirectory($"Том {volume.Key}").Name;
                foreach (var chapter in volume)
                {
                    if (chapterize)
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

        public async Task DownloadChapter(MangaChapter chapter, string path)
        {
            var url = "https://manga.in.ua/chapters/20438-ljudina-benzopila-tom-12-rozdil-100.html"; // chapter.URL;

            var html = await GetFullHTML(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            Console.WriteLine(doc.Text);
            
            //var pages = doc.DocumentNode.SelectNodes()
            /*using var client = new WebClient();
            var html = client.DownloadString(url);

            var pages = _li.Matches(_ul.Match(html).Value).Select(m => m.Groups[1].Value).ToList();

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var number = (i + 1).ToString().PadLeft(2, '0');
                var output = Path.Combine(_path, $"{number}{Path.GetExtension(page)}");
                client.DownloadFile($"{miu}{page}", output);
                Console.WriteLine($"[downloaded] \"{output}\"");
            }*/
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
    }
}