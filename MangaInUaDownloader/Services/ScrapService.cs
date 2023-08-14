using HtmlAgilityPack;
using MangaInUaDownloader.Utils.ConsoleExtensions;
using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using Spectre.Console;

namespace MangaInUaDownloader.Services
{
    public class ScrapService // todo rename this mf
    {
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0";

        public static readonly ScrapService Instance = new();

        private IBrowser? _browser;


        public async Task<IPage> OpenWebPageAsync(string url, IStatus status, string what)
        {
            if (_browser is null)
            {
                await OpenBrowser(status);
            }
            
            status.SetStatus($"Opening {what} page...");
            var page = await _browser!.NewPageAsync();

            await page.SetUserAgentAsync(USER_AGENT);
            await page.GoToAsync(url);

            return page;
        }

        private async Task OpenBrowser(IStatus status)
        {
            status.SetStatus("Fetching browser...");
            var relevant = new ValueWrapper<bool>(true);
            UpdateStatusOnWaiting(status, relevant);
            await FetchBrowser();
            relevant.Value = false;

            status.SetStatus("Launching browser...");
            var pupex = new PuppeteerExtra().Use(new StealthPlugin());
            _browser = await pupex.LaunchAsync(new LaunchOptions { Headless = true });

            AppDomain.CurrentDomain.ProcessExit += CloseBrowser;
        }

        private async void UpdateStatusOnWaiting(IStatus status, ValueWrapper<bool> relevant)
        {
            await Task.Delay(1500);
            if (relevant.Value)
            {
                status.SetStatus("Fetching browser... [darkorange](can take up to 30 seconds with newly installed app)[/]");
            }
        }

        private async Task FetchBrowser()
        {
            using var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
        }


        public async Task LoadElement(IPage page, string selector, IStatus status, string what)
        {
            status.SetStatus($"Loading {what}...");
            await page.WaitForSelectorAsync(selector);
        }
        
        public async Task Click(IPage page, string selector, IStatus status)
        {
            status.SetStatus("Waiting for a button...");
            await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions() { Visible = true });
            
            status.SetStatus("Clicking...");
            await page.ClickAsync(selector, new ClickOptions() { Delay = 95 });
        }


        public async Task<string> GetContent(IPage page)
        {
            var html = await page.GetContentAsync();
            DisposePage(page);
            return html;
        }
        private void DisposePage(IPage page) => page.DisposeAsync();


        public HtmlNodeCollection? GetHTMLNodes(string html, string selector)
        {
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

        /// <summary> Returns an HTML code of a page without loading any JS garbage. </summary>
        public HtmlDocument GetPlainHTML(string url)
        {
            return new HtmlWeb().Load(url);
        }

        private void CloseBrowser(object? sender, EventArgs e)
        {
            AnsiConsole.Status().Start("Closing browser...", _ => _browser?.DisposeAsync());
        }
    }
}