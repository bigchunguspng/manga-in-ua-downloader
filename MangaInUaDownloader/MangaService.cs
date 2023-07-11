using System.Net;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using PuppeteerSharp;

namespace MangaInUaDownloader
{
    public class MangaService
    {
        private const string miu = "https://manga.in.ua";
    
        private static readonly Regex _ul = new(@"<ul class=""xfieldimagegallery.*ul>");
        private static readonly Regex _li = new(@"<li.*?src=""(.*?)"".*?li>");
        private static readonly Regex _chapters = new(@"<div id=""linkstocomics"".*>");
        private static readonly Regex _chapter = new(@"");
        
        public async Task<List<TranslatedChapters>> ListTranslators(Uri url)
        {
            var html = await GetFullHTML(url.ToString()); // html with all chapters loaded

            Console.WriteLine("Scrapping information...");
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//div[@id='linkstocomics']//div[@class='ltcitems']");
            var chapters = nodes.Select(n => new MangaChapter()
            {
                Volume = Convert.ToInt32(n.Attributes["manga-tom"].Value),
                Chapter = Convert.ToSingle(n.Attributes["manga-chappter"].Value),
                Translator = n.Attributes["translate"].Value
            }).OrderBy(m => m.Chapter).GroupBy(m => m.Chapter).ToDictionary(g => g.Key, g => g.ToArray());

            var translations = new List<TranslatedChapters>();
            TranslatedChapters? dummy = null;
            foreach (var chapter in chapters)
            {
                if (dummy is null)
                {
                    dummy = ThisChapter();
                }
                else if (dummy.Translators.SequenceEqual(chapter.Value.Select(c => c.Translator)))
                {
                    dummy.ChapterB = chapter.Key;
                }
                else
                {
                    translations.Add(dummy);
                    dummy = ThisChapter();
                }
                
                TranslatedChapters ThisChapter() => new()
                {
                    ChapterA = chapter.Key, ChapterB = chapter.Key,
                    Translators = chapter.Value.Select(c => c.Translator).ToArray()
                };
            }

            if (dummy is not null) translations.Add(dummy);

            return translations;
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
            Console.WriteLine("Opening manga page...");
            await page.GoToAsync(url);
            Console.WriteLine("Loading chapters...");
            await page.WaitForSelectorAsync("div.ltcitems");
            return await page.GetContentAsync(); // html with all chapters loaded
        }
    }

    public class TranslatedChapters
    {
        public float ChapterA, ChapterB;
        public string[] Translators;
    }
    
    public class MangaChapter
    {
        public int Volume;
        public float Chapter; // chapters can be like "30.2"
        public string Translator, Title, URL;
    }
}