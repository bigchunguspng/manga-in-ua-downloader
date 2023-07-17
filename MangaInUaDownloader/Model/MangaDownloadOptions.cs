using MangaInUaDownloader.Utils;
using Range = MangaInUaDownloader.Utils.Range;

namespace MangaInUaDownloader.Model
{
    public record MangaDownloadOptions(RangeF Chapters, Range Volumes, string? Translator, bool DownloadOthers);
}