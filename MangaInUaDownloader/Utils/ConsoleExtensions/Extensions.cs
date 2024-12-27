using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace MangaInUaDownloader.Utils.ConsoleExtensions
{
    public static class Extensions
    {
        // PROGRESS TASK

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


        // CONFIG

        public static void HideDefaultValue(this HelpBuilder builder, Option option)
        {
            builder.CustomizeSymbol(option, secondColumnText: option.Description);
        }

        public static CommandLineBuilder UseParseErrorReporting(this CommandLineBuilder builder, HelpBuilder help)
        {
            builder.AddMiddleware(async (context, next) =>
            {
                if (context.ParseResult.Errors.Count > 0)
                    context.InvocationResult = new ParseErrorResult(help);
                else
                    await next(context);
            }, MiddlewareOrder.ErrorReporting);

            return builder;
        }


        // HELP BUILDER

        public static IEnumerable<T> RecurseWhileNotNull<T>(this T? source, Func<T, T?> next) where T : class
        {
            while (source is not null)
            {
                yield return source;

                source = next(source);
            }
        }

        internal static Argument? Argument(this Symbol symbol) => symbol switch
        {
            Option   option   => new Argument<int> { HelpName = option.ArgumentHelpName },
            Command  command  => command.Arguments.FirstOrDefault(),
            Argument argument => argument,
            _                 => throw new NotSupportedException()
        };
    }

    public class ValueWrapper<T>
    {
        public ValueWrapper(T value) { Value = value; }

        public T Value { get; set; }
    }
}