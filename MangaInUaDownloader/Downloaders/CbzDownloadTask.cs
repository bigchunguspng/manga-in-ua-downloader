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

        public CbzDownloadTask(string filename, string root)
        {
            Filename = filename;
            RootDirectory = root;
        }

        public override async Task Run(ProgressTask progress)
        {
            await base.Run(progress);

            progress.SetStatus("[olive]Archiving...[/]");

            var pattern = Chapterize ? "*.*" : $"{Chapter} - *.*";
            var pages = Directory.GetFiles(Location, pattern);
            var archive = Path.Combine(RootDirectory, Filename);

            await AddPagesToArchive(pages, archive);

            progress.SetStatus("[olive]Cleaning...[/]");
            foreach (var page in pages) File.Delete(page);

            DeleteEmptyDirectory(Location);
            if (Chapterize) DeleteEmptyDirectory(Path.GetDirectoryName(Location)!);

            progress.SetStatus("[green]Done ✓✓[/]");
        }

        private async Task AddPagesToArchive(string[] pages, string archive)
        {
            try
            {
                using var zip = ZipFile.Open(archive, ZipArchiveMode.Update);
                var skip = zip.Entries.Select(x => x.Name).ToList();
                foreach (var page in pages)
                {
                    var name = Path.GetFileName(page);
                    if (skip.Contains(name)) continue;

                    zip.CreateEntryFromFile(page, name);
                }
            }
            catch // archive can be used by another process >> try later
            {
                await Task.Delay(1000);
                await AddPagesToArchive(pages, archive);
            }
        }

        private void DeleteEmptyDirectory(string path)
        {
            if (DirectoryIsEmpty(path))
                Directory.Delete(path);
        }

        private bool DirectoryIsEmpty(string path) => !Directory.EnumerateFileSystemEntries(path).Any();
    }
}