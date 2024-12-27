using System.CommandLine;

namespace MangaInUaDownloader.Utils.ConsoleExtensions;

public class VersionOption : Option<bool>
{
    public VersionOption(string[] aliases) : base(aliases) { }

    public override string Description => Localization.Instance.VersionOptionDescription();

    public override bool Equals(object? obj) => obj is VersionOption;

    public override int GetHashCode() => typeof(VersionOption).GetHashCode();
}