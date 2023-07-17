using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MangaInUaDownloader.Model;

namespace MangaInUaDownloader.Services
{
    public class MangaInUaService : MangaService
    {
        //private const string miu = "https://manga.in.ua";

        private const string ALT = "Альтернативний переклад";
        private const string XPATH_CHAPTERS = "//div[@id='linkstocomics']//div[@class='ltcitems']";
        
        
        private static readonly Regex _chapter_url  = new(@"^https?:\/\/manga\.in\.ua\/chapters\/\S+");
        private static readonly Regex   _manga_url  = new(@"^https?:\/\/manga\.in\.ua\/mangas\/\S+");
    
        private static readonly Regex _title = new(@".+ - (.+)");

        public bool IsChapterURL(string url) => _chapter_url.IsMatch(url);
        public bool   IsMangaURL(string url) =>   _manga_url.IsMatch(url);

        public async Task<Dictionary<string, List<MangaChapter>>> GetTranslatorsByChapter(string url)
        {
            var html = await GetFullHTML(url);
            var nodes = GetAllChapters(html);

            var chapters = nodes.Select(ParseAsChapter).OrderBy(m => m.Chapter).ToList();
            
            FixNaming(chapters);

            return chapters.GroupBy(g => g.Title).ToDictionary(g => g.Key, g => g.ToList());
        }

        private void FixNaming(List<MangaChapter> chapters)
        {
            foreach (var c in chapters)
            {
                if (c.Title.Contains(ALT))
                {
                    c.IsAlternative = true;
                    c.Title = chapters.First(x => x.Chapter.Equals(c.Chapter) && !x.Title.Contains(ALT)).Title;
                }
                else
                {
                    var title = _title.Match(c.Title).Groups[1].Value;
                    c.Title = string.IsNullOrEmpty(title) ? MangaService.UNTITLED : title;
                }
            }
        }

        public async Task<IEnumerable<MangaChapter>> GetChapters(string url, MangaDownloadOptions options)
        {
            var html = await GetFullHTML(url);
            var nodes = GetAllChapters(html);

            var chapters = nodes
                .Select(ParseAsChapter)
                .OrderBy(m => m.Chapter)
                .Where(x =>
                    x.Volume  >= options. Volumes.Min &&  x.Volume  <= options. Volumes.Max &&
                    x.Chapter >= options.Chapters.Min &&  x.Chapter <= options.Chapters.Max)
                .ToList();

            FixNaming(chapters);
            
            // download others + tr => group by chap > select g.where tr = x
            // only tr? => select only where tr = tr
            // nothing specified? => take main trainslation
            if (options.Translator is not null)
            {
                if (options.DownloadOthers)
                {
                    return chapters
                        .GroupBy(x => x.Chapter)
                        .Select(g => g.Any(x => TranslatedBy(x, options.Translator))
                            ? g.First(x =>      TranslatedBy(x, options.Translator))
                            : g.First(x => !x.IsAlternative));
                }
                else
                {
                    return chapters.Where(x => TranslatedBy(x, options.Translator));
                }
            }
            else
            {
                return chapters.Where(x => !x.IsAlternative);
            }
        }

        private static async Task<string> GetFullHTML(string url)
        {
            var page = await ScrapService.Instance.OpenWebPageAsync(url, "manga");
            
            await ScrapService.Instance.LoadElement(page, "div.ltcitems", "chapters");

            return await ScrapService.Instance.GetContent(page); // html with all chapters loaded
        }

        private HtmlNodeCollection GetAllChapters(string html)
        {
            return ScrapService.Instance.GetHTMLNodes(html, XPATH_CHAPTERS, "Collecting chapters...");
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
}