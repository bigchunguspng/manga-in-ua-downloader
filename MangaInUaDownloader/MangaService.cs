using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MangaInUaDownloader.Commands;
using PuppeteerSharp;
using Range = MangaInUaDownloader.Commands.Range;

namespace MangaInUaDownloader
{
    public class MangaService
    {
        private const string miu = "https://manga.in.ua";
        private const string ALT = "Альтернативний переклад";
        public  const string UNTITLED = "Без назви";
    
        private static readonly Regex _ul = new(@"<ul class=""xfieldimagegallery.*ul>");
        private static readonly Regex _li = new(@"<li.*?src=""(.*?)"".*?li>");
        private static readonly Regex _chapters = new(@"<div id=""linkstocomics"".*>");
        private static readonly Regex _title = new(@".+ - (.+)");

        public async Task<List<TranslatedChapters>> GetTranslatorsByChapter(Uri url)
        {
            var html = await GetFullHTML(url.ToString(), "div.ltcitems"); // html with all chapters loaded

            var nodes = GetAllChapters(html);
            var chapters = nodes.Select(ParseAsChapter).OrderBy(m => m.Chapter).GroupBy(m => m.Chapter).ToDictionary(g => g.Key, g => g.ToList());

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

        public async Task<IEnumerable<MangaChapter>> GetChapters(Uri url, RangeF chapter, Range volume, string? translator, bool downloadOthers)
        {
            var html = await GetFullHTML(url.ToString(), "div.ltcitems");
            var nodes = GetAllChapters(html);

            var chapters = nodes
                .Select(ParseAsChapter)
                .OrderBy(m => m.Chapter)
                .Where(x =>
                    x.Volume  >=  volume.Min &&  x.Volume  <=  volume.Max &&
                    x.Chapter >= chapter.Min &&  x.Chapter <= chapter.Max)
                .ToList();

            foreach (var c in chapters)
            {
                if (c.Title.Contains(ALT))
                {
                    c.IsAlternative = true;
                    c.Title = chapters.First(x => x.Chapter.Equals(c.Chapter) && !x.Title.Contains(ALT)).Title;
                }

                var title = _title.Match(c.Title).Groups[1].Value;
                c.Title = string.IsNullOrEmpty(title) ? UNTITLED : title;
            }
            
            // download others + tr => group by chap > select g.where tr = x
            // only tr? => select only where tr = tr
            // nothing specified? => take main trainslation
            if (translator is not null)
            {
                if (downloadOthers)
                {
                    return chapters
                        .GroupBy(x => x.Chapter)
                        .Select(g => g.Any(x => TranslatedBy(x, translator)) ? g.First(x => TranslatedBy(x, translator)) : g.First(x => !x.IsAlternative));
                }
                else
                {
                    return chapters.Where(x => TranslatedBy(x, translator));
                }
            }
            else
            {
                return chapters.Where(x => !x.IsAlternative);
            }
        }

        public static async Task<string> GetFullHTML(string url, string selector)
        {
            Console.WriteLine("Fetching browser...");
            using var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
            Console.WriteLine("Launching Puppeteer...");
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = false });

            Console.WriteLine("Opening browser...");
            await using var page = await browser.NewPageAsync();
            Console.WriteLine("Opening manga page...");
            await page.GoToAsync(url);
            Console.WriteLine("Loading chapters...");
            await page.WaitForSelectorAsync(selector);
            return await page.GetContentAsync(); // html with all chapters loaded
        }

        private HtmlNodeCollection GetAllChapters(string html)
        {
            Console.WriteLine("Collecting chapters...");
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return doc.DocumentNode.SelectNodes("//div[@id='linkstocomics']//div[@class='ltcitems']");
        }

        private MangaChapter ParseAsChapter(HtmlNode node)
        {
            var a = node.ChildNodes["a"];
            return new MangaChapter()
            {
                Volume  = Convert.ToInt32 (node.Attributes["manga-tom"     ].Value),
                Chapter = Convert.ToSingle(node.Attributes["manga-chappter"].Value),
                Translator =               node.Attributes["translate"     ].Value,
                Title = a.InnerText,
                URL   = a.Attributes["href"].Value
            };
        }

        private bool TranslatedBy(MangaChapter chapter, string translator)
        {
            return chapter.Translator.Contains(translator, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public class TranslatedChapters
    {
        public float ChapterA, ChapterB; // chapters can have numbers like "30.2"
        public string[] Translators;
    }
    
    public class MangaChapter
    {
        public int Volume;
        public float Chapter;
        public bool IsAlternative;
        public string Translator, Title, URL;
    }
}