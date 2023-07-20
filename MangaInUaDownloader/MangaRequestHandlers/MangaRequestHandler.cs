using System.CommandLine.Invocation;

namespace MangaInUaDownloader.MangaRequestHandlers
{
    public abstract class MangaRequestHandler : ICommandHandler
    {
        public abstract string MANGA_WEBSITE { get; }
        
        public abstract bool CanHandleThis(string url);

        public abstract Task<int> InvokeAsync(InvocationContext context);
        
        public int Invoke(InvocationContext context) => InvokeAsync(context).Result;
    }
}