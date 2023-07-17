using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MangaInUaDownloader.Model;

namespace MangaInUaDownloader.Services
{
    public class MangaInUaService : MangaService
    {
        private const string ALT = "Альтернативний переклад";
        private const string XPATH_CHAPTERS = "//div[@id='linkstocomics']//div[@class='ltcitems']";
        private const string XPATH_PAGES = "//div[@id='comics']//ul[@class='xfieldimagegallery loadcomicsimages']//li//img";
        private const string SELECTOR_UL = "div#comics ul.xfieldimagegallery.loadcomicsimages";
        private const string SELECTOR_BUTTON = "div#startloadingcomicsbuttom a";

        private readonly Regex _title_xd = new(@"Том: (.+)\. Розділ: (.+?) .+");



        public bool IsChapterURL(string url) => Regex.IsMatch(url, @"^https?:\/\/manga\.in\.ua\/chapters\/\S+");
        public bool   IsMangaURL(string url) => Regex.IsMatch(url, @"^https?:\/\/manga\.in\.ua\/mangas\/\S+");
        
    
        private static readonly Regex _title = new(@".+ - (.+)");

        public async Task<Dictionary<string, List<MangaChapter>>> GetChaptersGrouped(string url)
        {
            var html = await GetMangaPageHTML(url);
            var nodes = GetAllChapterNodes(html);

            var chapters = nodes.Select(ChapterFromNode).OrderBy(m => m.Chapter).ToList();
            
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
            var html = await GetMangaPageHTML(url);
            var nodes = GetAllChapterNodes(html);

            var chapters = nodes
                .Select(ChapterFromNode)
                .OrderBy(m => m.Chapter)
                .Where(x => options.Volumes.Contains(x.Volume) && options.Chapters.Contains(x.Chapter))
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
                            ? g.First(     x => TranslatedBy(x, options.Translator))
                            : g.First(IsMainTranslation));
                }
                else
                {
                    return chapters.Where(x => TranslatedBy(x, options.Translator));
                }
            }
            else
            {
                return chapters.Where(IsMainTranslation);
            }
        }

        public async Task<List<string>> GetChapterPages(string url)
        {
            var html = await GetChapterPageHTML(url);
            var pages = ScrapService.Instance.GetHTMLNodes(html, XPATH_PAGES, "Collecting pages...");

            /*if (chapter.Volume < 0)
            {
                // get name and shit
                var title = ScrapService.Instance.GetHTMLNode(html, "//head//title").InnerText;
                var match = _title_xd.Match(title);
                chapter.Volume = Convert.ToInt32(match.Groups[1].Value);
                chapter.Chapter = Convert.ToSingle(match.Groups[2].Value);
                var v = CreateVolumePath(chapter.Volume);
                path = _chapterize ? CreateChapterPath(chapter, v) : v;
                chapter_number = $"{chapter.Chapter}";
            }*/

            return pages.Select(node => node.Attributes["data-src"].Value).ToList();
        }
        
        
        
        private async Task<string> GetChapterPageHTML(string url)
        {
            var page = await ScrapService.Instance.OpenWebPageAsync(url, "CHAPTER");
            
            await ScrapService.Instance.Click(page, SELECTOR_BUTTON);
            await ScrapService.Instance.LoadElement(page, SELECTOR_UL, "pages");
            
            return await ScrapService.Instance.GetContent(page);
        }

        private static async Task<string> GetMangaPageHTML(string url)
        {
            var page = await ScrapService.Instance.OpenWebPageAsync(url, "MANGA");
            
            await ScrapService.Instance.LoadElement(page, "div.ltcitems", "chapters");

            return await ScrapService.Instance.GetContent(page);
        }

        private HtmlNodeCollection GetAllChapterNodes(string html)
        {
            return ScrapService.Instance.GetHTMLNodes(html, XPATH_CHAPTERS, "Collecting chapters...");
        }

        private MangaChapter ChapterFromNode(HtmlNode node)
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

        
        private bool IsMainTranslation(MangaChapter x) => !x.IsAlternative;

        private bool TranslatedBy(MangaChapter chapter, string translator)
        {
            return chapter.Translator.Contains(translator, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}