namespace MangaInUaDownloader.Model
{
    public class MangaChapter
    {
        public int Volume;
        public float Chapter;
        public bool IsAlternative;
        public string Translator = null!, Title = null!, URL = null!;
    }

    public record MangaChapterNumber(int Volume, float Chapter);
}