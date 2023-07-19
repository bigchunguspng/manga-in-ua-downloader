using Spectre.Console;
using Spectre.Console.Rendering;

namespace MangaInUaDownloader.Utils.ConsoleExtensions
{
    public sealed class TaskNameColumn : ProgressColumn
    {
        /// <summary>
        /// Gets or sets the alignment of the task description.
        /// </summary>
        public Justify Alignment { get; set; } = Justify.Right;

        /// <inheritdoc/>
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            return new Markup(task.GetName()).Overflow(Overflow.Ellipsis).Justify(Alignment);
        }
    }
    
    public sealed class TaskStatusColumn : ProgressColumn
    {
        /// <summary>
        /// Gets or sets the alignment of the task description.
        /// </summary>
        public Justify Alignment { get; set; } = Justify.Left;

        /// <inheritdoc/>
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            return new Markup(task.GetStatus()).Overflow(Overflow.Ellipsis).Justify(Alignment);
        }
    }
    
    public sealed class PagesDownloadedColumn : ProgressColumn
    {

        /// <inheritdoc/>
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            var total = task.MaxValue;

            if (task.IsFinished)
            {
                return new Markup($"[green]pages: {total}[/]");
            }
            else
            {
                var downloaded = task.Value;

                return new Markup($"[grey]pages: [/]{downloaded}[grey]/[/]{total}");
            }
        }
    }

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