using Spectre.Console;

namespace MangaInUaDownloader.Utils.ConsoleExtensions
{
    public static class Extensions
    {
        public static string GetStatus(this ProgressTask task)
        {
            return task.HasStatus() ? task.Description.Substring(task.GetStatusIndex() + 1) : string.Empty;
        }
        public static string GetName(this ProgressTask task)
        {
            return task.HasStatus() ? task.Description.Remove(task.GetStatusIndex()) : task.Description;
        }
        
        public static void SetStatus(this ProgressTask task, string status)
        {
            var name = task.GetName();

            task.Description = status.Length > 0 ? $"{name}\t{status}" : name;
        }
        public static void SetName(this ProgressTask task, string name)
        {
            var status = task.GetStatus();
            
            task.Description = status.Length > 0 ? $"{name}\t{status}" : name;
        }

        private static int GetStatusIndex(this ProgressTask task) => task.Description.IndexOf('\t');

        private static bool HasStatus(this ProgressTask task) => task.GetStatusIndex() > -1;
    }
}