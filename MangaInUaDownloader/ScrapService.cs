using HtmlAgilityPack;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace MangaInUaDownloader
{
    public class ScrapService
    {
        public static readonly ScrapService Instance = new();

        private IBrowser? _browser; // todo dispose after all

        private ScrapService() => OpenBrowser().Wait();

        private async Task OpenBrowser()
        {
            await FetchBrowser();
            
            Console.WriteLine("Launching browser...");
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        }

        private async Task FetchBrowser()
        {
            Console.WriteLine("Fetching browser...");
            using var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
        }

        public async Task<IPage> OpenWebPageAsync(string url, string what)
        {
            Console.WriteLine($"Opening {what} page...");
            var page = await _browser!.NewPageAsync();

            await page.GoToAsync(url);

            return page;
        }

        public async Task LoadElement(IPage page, string selector, string what)
        {
            Console.WriteLine($"Loading {what}...");
            await page.WaitForSelectorAsync(selector);
        }
        
        public async Task Click(IPage page, string selector)
        {
            Console.WriteLine("Waiting for a button...");
            await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions() { Visible = true });
            Console.WriteLine("Clicking...");
            await page.ClickAsync(selector, new ClickOptions() { Delay = 95 });
        }

        public async Task<string> GetContent(IPage page)
        {
            var html = await page.GetContentAsync();
            DisposePage(page); // try todo not await
            return html;
        }

        private void DisposePage(IPage page) => page.DisposeAsync();
        
        public HtmlNodeCollection GetHTMLNodes(string html, string selector, string message)
        {
            Console.WriteLine(message);
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return doc.DocumentNode.SelectNodes(selector);
        }

        public void Dispose()
        {
            _browser?.DisposeAsync();
        }
    }
}