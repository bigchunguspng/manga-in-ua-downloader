using Spectre.Console;

namespace MangaInUaDownloader.Utils.ConsoleExtensions
{
    public interface IStatus
    {
        public void SetStatus(string status);
    }


    public class ProgressStatus : IStatus
    {
        private readonly ProgressTask _progress;
        private readonly string _color;

        public ProgressStatus(ProgressTask progress, string color = "yellow")
        {
            _progress = progress;
            _color = color;
        }


        public void SetStatus(string status)
        {
            _progress.SetStatus($"[{_color}]{status}[/]");
        }
    }


    public class StatusStatus : IStatus
    {
        private readonly StatusContext _context;
        private readonly string _color;

        public StatusStatus(StatusContext context, string color = "default")
        {
            _context = context;
            _color = color;
        }


        public void SetStatus(string status)
        {
            _context.Status = $"[{_color}]{status}[/]";
        }
    }


    public class ConsoleStatus : IStatus
    {
        public void SetStatus(string status)
        {
            AnsiConsole.MarkupLine(status);
        }
    }


    public class SilentStatus : IStatus
    {
        public void SetStatus(string status) { }
    }
}