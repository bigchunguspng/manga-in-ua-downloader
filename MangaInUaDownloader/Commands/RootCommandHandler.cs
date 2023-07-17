using System.CommandLine.Invocation;
using MangaInUaDownloader.MangaRequestHandlers;

namespace MangaInUaDownloader.Commands
{
    public class RootCommandHandler : ICommandHandler
    {
        private List<MangaRequestHandler>? MangaHandlers;
        
        private string? URL;

        public RootCommandHandler WithTheseSubhandlers(List<MangaRequestHandler> list)
        {
            MangaHandlers = list;

            return this;
        }

        public int Invoke(InvocationContext context) => InvokeAsync(context).Result;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            URL = context.ParseResult.GetValueForArgument(RootCommandBuilder.URLArg).ToString();

            if (MangaHandlers is null)
            {
                // todo cw explanation
                
                return -1;
            }
            
            foreach (var handler in MangaHandlers)
            {
                if (handler.CanHandleThis(URL))
                {
                    return await handler.InvokeAsync(context);
                }
            }
            // todo WRONG URL

            return -1;
        }
    }
}