using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

namespace AnkiCleaner.DependencyInjection;

internal sealed class TypeRegistrar(IHostBuilder builder) : ITypeRegistrar
{
    public ITypeResolver Build()
    {
        return new TypeResolver(builder.Build());
    }

    public void Register(Type service, Type implementation)
    {
        _ = builder.ConfigureServices(
            (_, services) => services.AddSingleton(service, implementation)
        );
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _ = builder.ConfigureServices(
            (_, services) => services.AddSingleton(service, implementation)
        );
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _ = builder.ConfigureServices(
            (_, services) => services.AddSingleton(service, _ => factory())
        );
    }
}
