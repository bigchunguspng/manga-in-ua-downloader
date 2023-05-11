using System.Net;
using System.Text.RegularExpressions;

#pragma warning disable SYSLIB0014

namespace MangaInUaDownloader
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console. InputEncoding = System.Text.Encoding.Unicode;

            new InputParser().Run(args);
        }
    }

    public class InputParser // miu-dl [-p "Title\Chapter"] URL
    {
        private readonly Regex _url  = new(@"https?:\/\/manga\.in\.ua\/\S+");

        public void Run(string[] input)
        {
            var path = input.Length > 2 && input[0] == "-p" ? input[1] : Environment.CurrentDirectory;

            var url = _url.IsMatch(input[^1]) ? input[^1] : throw new ArgumentException("No URL speified");
            
            new Downloader(path).Download(url);
        }
    }
    public class Downloader
    {
        private const string miu = "https://manga.in.ua";

        private static readonly Regex _ul = new(@"<ul class=""xfieldimagegallery.*ul>");
        private static readonly Regex _li = new(@"<li.*?src=""(.*?)"".*?li>");

        private readonly string _path;

        public Downloader(string path) => _path = Path.Combine(path.Split('\\', '/'));

        public void Download(string url)
        {
            Directory.CreateDirectory(_path);

            using var client = new WebClient();
            var html = client.DownloadString(url);

            var pages = _li.Matches(_ul.Match(html).Value).Select(m => m.Groups[1].Value).ToList();

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var number = (i + 1).ToString().PadLeft(2, '0');
                var output = Path.Combine(_path, $"{number}{Path.GetExtension(page)}");
                client.DownloadFile($"{miu}{page}", output);
                Console.WriteLine($"[downloaded] \"{output}\"");
            }
        }
    }
}