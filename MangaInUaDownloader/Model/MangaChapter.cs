namespace MangaInUaDownloader.Model
{
    public class MangaChapter
    {
        public int    Volume;
        public float  Chapter;
        public string Title      = null!;
        public string URL        = null!;
        public string Translator = null!;
        public bool   IsAlternative;
    }

    public record MangaChapterNumber(int Volume, float Chapter);
}