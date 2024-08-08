using System.Runtime.InteropServices;
using HtmlAgilityPack;
using MangaInUaDownloader.Utils;
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
        private bool _userAgentSet;


        public async Task<IPage> OpenWebPageAsync(string url, IStatus status, string what)
        {
            if (_browser is null)
            {
                await OpenBrowser(status);
            }
            
            status.SetStatus($"Opening {what} page...");
            var page = (await _browser!.PagesAsync()).First();

            if (_userAgentSet == false)
            {
                await page.SetUserAgentAsync(USER_AGENT);
                _userAgentSet = true;
            }

            await page.GoToAsync(url);

            return page;
        }

        private async Task OpenBrowser(IStatus status)
        {
            status.SetStatus("Fetching browser...");
            var exePath = TryToFindBrowser();
            var download = exePath is null;
            if (download)
            {
                var relevant = new ValueWrapper<bool>(true);
                UpdateStatusOnWaiting(status, relevant);
                await FetchBrowser();
                relevant.Value = false;
            }

            status.SetStatus("Launching browser...");
            var options = new LaunchOptions { Headless = true };
            if (exePath != null)
            {
                var chrome = exePath.EndsWith("chrome.exe");
                var msedge = exePath.EndsWith("msedge.exe");
                options.ExecutablePath = exePath;
                options.Browser = chrome
                    ? SupportedBrowser.Chrome
                    : msedge
                        ? SupportedBrowser.Chromium
                        : SupportedBrowser.Firefox;
            }

            var pupex = new PuppeteerExtra().Use(new StealthPlugin());
            _browser = await pupex.LaunchAsync(options);

            AppDomain.CurrentDomain.ProcessExit += CloseBrowser;
        }

        private string? TryToFindBrowser()
        {
            if (Helpers.GetCurrentPlatform() != OSPlatform.Windows) return null;

            var pf64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var appd = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            const string chrm = @"Google\Chrome\Application\chrome.exe";
            const string edge = @"Microsoft\Edge\Application\msedge.exe";
          //const string fire = @"Mozilla Firefox\firefox.exe";

            var paths = new[]
            {
                $@"{pf64}\{chrm}", $@"{pf86}\{chrm}", $@"{appd}\{chrm}",
                $@"{pf86}\Google\Application\chrome.exe",
                $@"{pf64}\{edge}", $@"{pf86}\{edge}", $@"{appd}\{edge}",
              //$@"{pf64}\{fire}", $@"{pf86}\{fire}", $@"{appd}\{fire}", // <-- might not work
            };

            var path = paths.FirstOrDefault(File.Exists);
            if (path is not null) AnsiConsole.MarkupLine($"[dim]Using \"{path}\"[/]");

            return path;
        }

        private async void UpdateStatusOnWaiting(IStatus status, ValueWrapper<bool> relevant)
        {
            await Task.Delay(1500);
            if (relevant.Value)
            {
                status.SetStatus("Fetching browser... [darkorange](can take up to 30 seconds with a newly installed app)[/]");
            }
        }

        private async Task FetchBrowser()
        {
            var browserFetcher = new BrowserFetcher();
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


        public Task<string> GetContent(IPage page)
        {
            return page.GetContentAsync();
        }


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