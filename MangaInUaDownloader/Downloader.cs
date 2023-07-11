using System.Net;
using System.Text.RegularExpressions;

#pragma warning disable SYSLIB0014

namespace MangaInUaDownloader
{
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