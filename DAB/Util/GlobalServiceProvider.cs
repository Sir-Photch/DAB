using DAB.Util.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace DAB.Util;

internal static class GlobalServiceProvider
{
    internal static IServiceProvider? _provider;

    internal static void Init(IServiceProvider provider) => _provider = provider;

    internal static T? GetService<T>(bool @throw = true) where T : class
    {
        T? service = _provider?.GetService<T>();
        if (service is null && @throw)
            throw new MissingServiceException($"{typeof(T).Name} is missing from {nameof(_provider)}");

        return service;
    }
}
