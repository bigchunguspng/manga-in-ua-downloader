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

        public ProgressStatus(ProgressTask progress) => _progress = progress;


        public void SetStatus(string status)
        {
            _progress.SetStatus(status);
        }
    }


    public class StatusStatus : IStatus
    {
        private readonly StatusContext _context;

        public StatusStatus(StatusContext context) => _context = context;


        public void SetStatus(string status)
        {
            _context.Status = status;
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