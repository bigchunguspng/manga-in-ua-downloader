using HtmlAgilityPack;
using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace MangaInUaDownloader.Services
{
    public class ScrapService // todo rename this mf
    {
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0";
        
        public static readonly ScrapService Instance = new();

        private IBrowser? _browser; // todo dispose after all

        private ScrapService() => OpenBrowser().Wait();

        private async Task OpenBrowser()
        {
            await FetchBrowser();
            
            Console.WriteLine("Launching browser...");
            var pup = new PuppeteerExtra().Use(new StealthPlugin());
            _browser = await pup.LaunchAsync(new LaunchOptions { Headless = false });
            
            AppDomain.CurrentDomain.ProcessExit += CloseBrowser;
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

            await page.SetUserAgentAsync(USER_AGENT);
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

        public HtmlNode GetHTMLNode(string html, string selector)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            return doc.DocumentNode.SelectSingleNode(selector);
        }
        public HtmlNode GetHTMLNode(HtmlDocument html, string selector)
        {
            return html.DocumentNode.SelectSingleNode(selector);
        }

        /// <summary> Returns an HTML code of a page without loading any JS garbage. </summary>
        public HtmlDocument GetPlainHTML(string url)
        {
            return new HtmlWeb().Load(url);
        }

        private void CloseBrowser(object? sender, EventArgs e)
        {
            Console.WriteLine("Closing browser...");
            _browser?.DisposeAsync();
        }
    }
}