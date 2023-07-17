using System.CommandLine.Invocation;

namespace MangaInUaDownloader.MangaRequestHandlers
{
    public abstract class MangaRequestHandler : ICommandHandler
    {
        public abstract bool CanHandleThis(string url);
        
        public int Invoke(InvocationContext context) => InvokeAsync(context).Result;

        public abstract Task<int> InvokeAsync(InvocationContext context);
    }
}