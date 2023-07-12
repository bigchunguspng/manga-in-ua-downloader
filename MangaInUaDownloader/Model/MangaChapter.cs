namespace MangaInUaDownloader.Model
{
    public class MangaChapter
    {
        public int Volume;
        public float Chapter;
        public bool IsAlternative;
        public string Translator, Title, URL;
    }
    
    public class TranslatedChapters
    {
        public float ChapterA, ChapterB; // chapters can have numbers like "30.2"
        public string[] Translators;
    }
}