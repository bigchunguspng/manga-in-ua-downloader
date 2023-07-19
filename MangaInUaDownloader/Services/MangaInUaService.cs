using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MangaInUaDownloader.Model;
using MangaInUaDownloader.Utils.ConsoleExtensions;

namespace MangaInUaDownloader.Services
{
    public class MangaInUaService : MangaService
    {
        private const string ALT = "Альтернативний переклад";
        private const string XPATH_CHAPTERS = "//div[@id='linkstocomics']//div[@class='ltcitems']";
        private const string XPATH_PAGES = "//div[@id='comics']//ul[@class='xfieldimagegallery loadcomicsimages']//li//img";
        private const string XPATH_THIS_CHAPTER = "//div[@id='site-content']//div[@class='youreadnow']";
        private const string XPATH_HEAD_TITLE = "//head//title";
        private const string SELECTOR_UL = "div#comics ul.xfieldimagegallery.loadcomicsimages";
        private const string SELECTOR_BUTTON = "div#startloadingcomicsbuttom a";

        private readonly Regex _manga_title_head = new(@"Манґа (.+) читати українською");
        private readonly Regex _manga_title_rn = new(@"Читати: (.+?) - ");
        private readonly Regex _tom_rozdil = new(@"Том: (.+)\. Розділ: (\d+(?:\.\d+)?)(?: - (.+))?");


        public bool IsChapterURL(string url) => Regex.IsMatch(url, @"^https?:\/\/manga\.in\.ua\/chapters\/\S+");
        public bool   IsMangaURL(string url) => Regex.IsMatch(url, @"^https?:\/\/manga\.in\.ua\/mangas\/\S+");
        
    
        private static readonly Regex _chapter_title = new(@".+ - (.+)");

        public async Task<Dictionary<string, List<MangaChapter>>> GetChaptersGrouped(string url, IStatus status)
        {
            var html = await GetMangaPageHTML(url, status);
            var nodes = GetAllChapterNodes(html, status);

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
                    var title = _chapter_title.Match(c.Title).Groups[1].Value;
                    c.Title = string.IsNullOrEmpty(title) ? MangaService.UNTITLED : title;
                }
            }
        }

        public async Task<IEnumerable<MangaChapter>> GetChapters(string url, MangaDownloadOptions options, IStatus status)
        {
            var html = await GetMangaPageHTML(url, status);
            var nodes = GetAllChapterNodes(html, status);

            var chapters = nodes
                .Select(ChapterFromNode)
                .OrderBy(m => m.Chapter)
                .Where(x => options.Volumes.Contains(x.Volume) && options.Chapters.Contains(x.Chapter))
                .ToList();

            FixNaming(chapters);
            
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
                else return chapters.Where(x => TranslatedBy(x, options.Translator));
            }
            else return chapters.Where(IsMainTranslation);
        }

        public async Task<List<string>> GetChapterPages(string url, IStatus status)
        {
            var html = await GetChapterPageHTML(url, status);
            var pages = GetAllPagesNodes(html, status);

            return pages.Select(node => node.Attributes["data-src"].Value).ToList();
        }

        public async Task<string> GetMangaTitle(string url, IStatus status)
        {
            if (IsChapterURL(url))
            {
                var html = await GetChapterPageHTML(url, status);
                var node = ScrapService.Instance.GetHTMLNode(html, XPATH_THIS_CHAPTER);

                return _manga_title_rn.Match(node.InnerText).Groups[1].Value;
            }
            else
            {
                var html = await GetMangaPageHTML(url, status);
                var node = ScrapService.Instance.GetHTMLNode(html, XPATH_HEAD_TITLE);
                
                return _manga_title_head.Match(node.InnerText).Groups[1].Value;
            }
        }

        public async Task<MangaChapter> GetChapterDetails(string url, IStatus status)
        {
            var html = await GetChapterPageHTML(url, status);
            var node = ScrapService.Instance.GetHTMLNode(html, XPATH_THIS_CHAPTER);

            var match = _tom_rozdil.Match(node.InnerText);
            return new MangaChapter
            {
                Volume  = Convert.ToInt32 (match.Groups[1].Value),
                Chapter = Convert.ToSingle(match.Groups[2].Value),
                Title = match.Groups[3].Success ? match.Groups[3].Value : MangaService.UNTITLED
            };
        }


        private (string URL, string HTML)? _mangaHTML, _chapterHTML;

        private async Task<string> GetMangaPageHTML(string url, IStatus status)
        {
            if (_mangaHTML is null || _mangaHTML.Value.URL != url)
            {
                var page = await ScrapService.Instance.OpenWebPageAsync(url, status, "manga");
                await ScrapService.Instance.LoadElement(page, "div.ltcitems", status, "chapters");
                var html = await ScrapService.Instance.GetContent(page);

                _mangaHTML = (url, html);
            }
            return _mangaHTML.Value.HTML;
        }

        private async Task<string> GetChapterPageHTML(string url, IStatus status)
        {
            if (_chapterHTML is null || _chapterHTML.Value.URL != url)
            {
                var page = await ScrapService.Instance.OpenWebPageAsync(url, status, "chapter");
                await ScrapService.Instance.Click(page, SELECTOR_BUTTON, status);
                await ScrapService.Instance.LoadElement(page, SELECTOR_UL, status, "pages");
                var html = await ScrapService.Instance.GetContent(page);

                _chapterHTML = (url, html);
            }
            return _chapterHTML.Value.HTML;
        }

        private HtmlNodeCollection GetAllChapterNodes(string html, IStatus status)
        {
            status.SetStatus("Collecting chapters...");
            
            return ScrapService.Instance.GetHTMLNodes(html, XPATH_CHAPTERS);
        }
        
        private HtmlNodeCollection GetAllPagesNodes(string html, IStatus status)
        {
            status.SetStatus("Collecting pages...");
            
            return ScrapService.Instance.GetHTMLNodes(html, XPATH_PAGES);
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