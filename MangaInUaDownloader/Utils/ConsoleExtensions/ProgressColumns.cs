using Spectre.Console;
using Spectre.Console.Rendering;

namespace MangaInUaDownloader.Utils.ConsoleExtensions
{
    public sealed class TaskNameColumn : ProgressColumn
    {
        /// <summary> Gets or sets the alignment of the task name. </summary>
        public Justify Alignment { get; set; } = Justify.Right;

        /// <inheritdoc/>
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            return new Markup(task.GetName()).Overflow(Overflow.Ellipsis).Justify(Alignment);
        }
    }
    
    public sealed class TaskStatusColumn : ProgressColumn
    {
        /// <summary> Gets or sets the alignment of the task status. </summary>
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
}