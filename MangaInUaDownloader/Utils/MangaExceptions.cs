namespace MangaInUaDownloader.Utils
{
    public class MangaNotFoundException : Exception
    {
        public MangaNotFoundException(string message) : base(message) { }
    }
}