using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MangaInUaDownloader.Model;
using MangaInUaDownloader.Utils;
using Range = MangaInUaDownloader.Utils.Range;

namespace MangaInUaDownloader.Services
{
    public class MangaService
    {
        private const string miu = "https://manga.in.ua";

        private const string ALT = "Альтернативний переклад";
        private const string XPATH_CHAPTERS = "//div[@id='linkstocomics']//div[@class='ltcitems']";
        public  const string UNTITLED = "(Без назви)";
    
        private static readonly Regex _title = new(@".+ - (.+)");

        public async Task<Dictionary<string, List<MangaChapter>>> GetTranslatorsByChapter(Uri url)
        {
            var html = await GetFullHTML(url.ToString());
            var nodes = GetAllChapters(html);

            var chapters = nodes.Select(ParseAsChapter).OrderBy(m => m.Chapter).ToList();
            
            FixNaming(chapters);

            return chapters.GroupBy(g => g.Title).ToDictionary(g => g.Key, g => g.ToList());

            /*var translations = new List<TranslatedChapters>();
            TranslatedChapters? dummy = null; // todo return mangachap list grouped by translators array
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

            if (dummy is not null) translations.Add(dummy);*/

            //return chapters;
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
                    c.Title = string.IsNullOrEmpty(title) ? UNTITLED : title;
                }
            }
        }

        public async Task<IEnumerable<MangaChapter>> GetChapters(Uri url, RangeF chapter, Range volume, string? translator, bool downloadOthers)
        {
            var html = await GetFullHTML(url.ToString());
            var nodes = GetAllChapters(html);

            var chapters = nodes
                .Select(ParseAsChapter)
                .OrderBy(m => m.Chapter)
                .Where(x =>
                    x.Volume  >=  volume.Min &&  x.Volume  <=  volume.Max &&
                    x.Chapter >= chapter.Min &&  x.Chapter <= chapter.Max)
                .ToList();

            FixNaming(chapters);
            
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