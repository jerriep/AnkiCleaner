using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

namespace AnkiCleaner.DependencyInjection;

internal sealed class TypeResolver(IHost host) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type)
    {
        return type is not null ? host.Services.GetRequiredService(type) : null;
    }

    public void Dispose()
    {
        host.Dispose();
    }
}
