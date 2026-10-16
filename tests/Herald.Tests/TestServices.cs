using Microsoft.Extensions.DependencyInjection;

namespace Herald.Tests;

internal static class TestServices
{
    public static ServiceProvider Build(Action<HeraldConfiguration>? configure = null)
        => new ServiceCollection()
            .AddSingleton<CallLog>()
            .AddHerald(configuration =>
            {
                configuration.RegisterServicesFromAssemblyContaining<CallLog>();
                configure?.Invoke(configuration);
            })
            .BuildServiceProvider();
}
