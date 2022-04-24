using DAB.Discord.Abstracts;

namespace DAB.Discord.HandlerModules;

internal class HandlerCollection<T>
{
    internal readonly IReadOnlyList<AbstractHandlerModule<T>> _handlers;

    internal HandlerCollection(IEnumerable<AbstractHandlerModule<T>> enHandlers)
    {
        _handlers = enHandlers.ToList();
    }

    internal HandlerCollection(params AbstractHandlerModule<T>[] handlers) : this(enHandlers: handlers) { }

    internal async Task<bool> HandleAsync(T context)
    {
        foreach (var handler in _handlers)
            handler.Context = context;

        return (await Task.WhenAll(_handlers.Select(h => h.HandleAsync()))).Any(x => x);
    }
}
