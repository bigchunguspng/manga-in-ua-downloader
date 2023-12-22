using System.IO.Compression;
using MangaInUaDownloader.Utils.ConsoleExtensions;
using Spectre.Console;

namespace MangaInUaDownloader.Downloaders
{
    /// <summary> Downloads all chapter's pages and adds them to a specified cbz-archive. </summary>
    public class CbzDownloadTask : RawDownloadTask
    {
        private readonly string Filename;
        private readonly string RootDirectory;

        public CbzDownloadTask(List<string> links, string path, float chapter, bool chapterize, string filename, string root) : base(links, path, chapter, chapterize)
        {
            Filename = filename;
            RootDirectory = root;
        }

        public override async Task Run(ProgressTask progress)
        {
            await base.Run(progress);

            progress.SetStatus("[olive]Archiving...[/]");

            var pages = Directory.GetFiles(Location, Chapterize ? "*.*" : $"{Chapter} - *.*");

            var archive = Path.Combine(RootDirectory, Filename);

            using var zip = ZipFile.Open(archive, ZipArchiveMode.Update);
            var skip = zip.Entries.Select(x => x.Name).ToList();
            foreach (var page in pages)
            {
                var name = Path.GetFileName(page);
                if (skip.Contains(name)) continue;

                zip.CreateEntryFromFile(page, name);
            }

            progress.SetStatus("[olive]Cleaning...[/]");
            foreach (var page in pages) File.Delete(page);

            DeleteEmptyDirectory(Location);
            if (Chapterize) DeleteEmptyDirectory(Path.GetDirectoryName(Location)!);

            progress.SetStatus("[green]Done ✓✓[/]");
        }

        private void DeleteEmptyDirectory(string path)
        {
            if (DirectoryIsEmpty(path))
                Directory.Delete(path);
        }

        private bool DirectoryIsEmpty(string path) => !Directory.EnumerateFileSystemEntries(path).Any();
    }
}