using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MangaInUaDownloader.Model;
using MangaInUaDownloader.Utils;
using MangaInUaDownloader.Utils.ConsoleExtensions;
using PuppeteerSharp;

namespace MangaInUaDownloader.Services
{
    public class MangaInUaService : IMangaService
    {
        private const string MAIN_PAGE = "https://manga.in.ua/";
        private const string ALT = "Альтернативний переклад";

        private const string XPATH_CHAPTERS = "//div[@id='linkstocomics']//div[@class='ltcitems']";
        private const string XPATH_PAGES = "//div[@id='comics']//ul[@class='xfieldimagegallery loadcomicsimages']//li//img";
        private const string XPATH_HEAD_TITLE = "//head//title";
        private const string XPATH_SEARCH_RESULT_ITEM = "//article[@class='item']";

        private const string SELECTOR_UL = "div#comics ul.xfieldimagegallery.loadcomicsimages";
        private const string SELECTOR_BUTTON = "div#startloadingcomicsbuttom a";

        private const string NOT_FOUND  = " can't be reached. Check the URL or try using the \"--slow\" option.";
        private const string MANGA_NOT_FOUND =           "Manga" + NOT_FOUND;
        private const string CHAPTER_NOT_FOUND = "Manga chapter" + NOT_FOUND;

        private readonly Regex _manga_title_head = new(@"(.+) читати українською");
        private readonly Regex _chapter_manga_title = new(@"^(.+?) - ");
        private readonly Regex _chapter_tom_rozdil = new(@"Том: (.+)\. Розділ: (\d+(?:[\.,]\d+)?)(?: - (.+))? читати українською");
        private readonly Regex _chapter_title = new(@".+ - (.+)");


        public bool IsChapterURL(string url) => Regex.IsMatch(url, @"^https?:\/\/manga\.in\.ua\/chapters\/\S+");
        public bool   IsMangaURL(string url) => Regex.IsMatch(url, @"^https?:\/\/manga\.in\.ua\/mangas\/\S+");


        public Task<List<MangaSearchResult>> Search(string query, IStatus status)
        {
            status.SetStatus("Searching...");
            
            var url = $"{MAIN_PAGE}?do=search&subaction=search&story={query}";
            var html = ScrapService.Instance.GetPlainHTML(url);
            var nodes = ScrapService.Instance.GetHTMLNodes(html.Text, XPATH_SEARCH_RESULT_ITEM);
            if (nodes is null)
            {
                return Task.FromResult(new List<MangaSearchResult>());
            }

            var list = new List<MangaSearchResult>(nodes.Count);

            foreach (var node in nodes)
            {
                var card = node.ChildNodes["div"];
                var progress = card.ChildNodes["header"].ChildNodes[^2].InnerText;
                var a = card.ChildNodes["main"].ChildNodes["h3"].ChildNodes["a"];
                var link = a.Attributes["href"].Value;
                var title = a.Attributes["title"].Value;
                var titles = title.Contains('/') ? title.Split('/', 2) : new[] { title, string.Empty };

                var item = new MangaSearchResult()
                {
                    TitleUkr = titles[0].Trim(),
                    TitleEng = titles[1].Trim(),
                    URL = link,
                    Progress = progress.Trim()
                };
                list.Add(item);
            }

            return Task.FromResult(list);
        }
        
        
        public async Task<List<List<MangaChapter>>> GetTranslations(string url, IStatus status)
        {
            var chapters = (await GetChapters(url, status)).ToList();
            
            FixNaming(chapters);

            return chapters.GroupBy(g => new MangaChapterNumber(g.Volume, g.Chapter)).Select(g => g.ToList()).ToList();
        }

        public async Task<IEnumerable<MangaChapter>> GetChapters(string url, IStatus status, MangaDownloadOptions options)
        {
            var chapters = (await GetChapters(url, status)).Where(options.ChapterNumberIsSatisfying).ToList();

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


        private async Task<IEnumerable<MangaChapter>> GetChapters(string url, IStatus status)
        {
            var html = await GetMangaPageHTML(url, status);
            var nodes = GetAllChapterNodes(html, status);

            return nodes.Select(ChapterFromNode).OrderBy(m => m.Chapter);
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
                    c.Title = string.IsNullOrEmpty(title) ? IMangaService.UNTITLED : title;
                }
            }
        }


        public async Task<List<string>> GetChapterPages(string url, IStatus status)
        {
            var html = await GetChapterPageHTML(url, status);
            var pages = GetAllPagesNodes(html, status);

            return pages.Select(node => node.Attributes["data-src"].Value).ToList();
        }

        public async Task<MangaChapter> GetChapterDetails(string url, IStatus status)
        {
            var html = await GetChapterPageHTML(url, status);
            var node = GetPageTitle(html);

            var match = _chapter_tom_rozdil.Match(node.InnerText);
            return new MangaChapter
            {
                Volume  = Convert.ToInt32 (match.Groups[1].Value),
                Chapter = Convert.ToSingle(match.Groups[2].Value),
                Title = match.Groups[3].Success ? match.Groups[3].Value : IMangaService.UNTITLED
            };
        }

        public async Task<string> GetMangaTitle(string url, IStatus status)
        {
            if (IsChapterURL(url))
            {
                var html = await GetChapterPageHTML(url, status);
                var node = GetPageTitle(html);

                return _chapter_manga_title.Match(node.InnerText).Groups[1].Value;
            }
            else
            {
                var html = await GetMangaPageHTML(url, status);
                var node = GetPageTitle(html);
                
                return DeTypedTitle(_manga_title_head.Match(node.InnerText).Groups[1].Value);
            }
        }


        private (string URL, string HTML)? _mangaHTML, _chapterHTML;


        private async Task<string> GetMangaPageHTML(string url, IStatus status)
        {
            if (_mangaHTML is null || _mangaHTML.Value.URL != url)
            {
                var page = await ScrapService.Instance.OpenWebPageAsync(url, status, "manga");
                await CheckForNotFound(page, _manga_title_head, MANGA_NOT_FOUND);

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
                await CheckForNotFound(page, _chapter_tom_rozdil, CHAPTER_NOT_FOUND);

                await ScrapService.Instance.Click(page, SELECTOR_BUTTON, status);
                await ScrapService.Instance.LoadElement(page, SELECTOR_UL, status, "pages");
                var html = await ScrapService.Instance.GetContent(page);

                _chapterHTML = (url, html);
            }
            return _chapterHTML.Value.HTML;
        }

        private async Task CheckForNotFound(IPage page, Regex regex, string message)
        {
            var node = GetPageTitle(await page.GetContentAsync());
            if (!regex.IsMatch(node.InnerText))
                throw new MangaNotFoundException(message);
        }


        private HtmlNode GetPageTitle(string html) => ScrapService.Instance.GetHTMLNode(html, XPATH_HEAD_TITLE);

        private HtmlNodeCollection GetAllChapterNodes(string html, IStatus status)
        {
            status.SetStatus("Collecting chapters...");
            
            return ScrapService.Instance.GetHTMLNodes(html, XPATH_CHAPTERS)!;
        }
        
        private HtmlNodeCollection GetAllPagesNodes(string html, IStatus status)
        {
            status.SetStatus("Collecting pages...");
            
            return ScrapService.Instance.GetHTMLNodes(html, XPATH_PAGES)!;
        }


        private MangaChapter ChapterFromNode(HtmlNode node)
        {
            var a = node.ChildNodes["a"];
            return new MangaChapter()
            {
                Volume  = Convert.ToInt32 (node.Attributes["manga-tom"     ].Value),
                Chapter = Convert.ToSingle(node.Attributes["manga-chappter"].Value),
                Translator =               node.Attributes["translate"     ].Value,
                Title = a.InnerText.Replace("...", "…"),
                URL   = a.Attributes["href"].Value
            };
        }

        private bool IsMainTranslation(MangaChapter x) => !x.IsAlternative;

        private bool TranslatedBy(MangaChapter chapter, string translator)
        {
            return chapter.Translator.Contains(translator, StringComparison.InvariantCultureIgnoreCase);
        }

        private string DeTypedTitle(string title)
        {
            var types = new[] { "Манґа", "Ваншот", "Манхва", "Маньхуа", "Вебманхва", "Додзінсі", "Доджінші" };

            return types.Any(title.StartsWith) ? title.Split(' ', 2)[1] : title;
        }
    }
}