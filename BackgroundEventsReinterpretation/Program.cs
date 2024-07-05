using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Hello, World!");

await UseMsExtHosting();

return; 

async ValueTask UseMsExtHosting()
{
    IHost host = new HostBuilder()
        .ConfigureServices((hostContext, services) =>
        {
            services
                .AddSingleton<IBackgroundTaskListener, HostedBackgroundTaskListener>()
                .AddHostedService<HostedBackgroundTaskListener>()
                .AddSingleton<EventCountConfiguration>()
                .AddSingleton<IBackgroundTask, EvenCount>()
                .AddSingleton<IBackgroundTask, OddCount>();
        })
        .Build();

    await host.RunAsync();
}

public interface IBackgroundTaskListener
{
    ValueTask ExecuteRegisteredJobs(CancellationToken cancellationToken);

}

public sealed class HostedBackgroundTaskListener(IEnumerable<IBackgroundTask> backgroundTasks) 
    : BackgroundService, IBackgroundTaskListener
{
    readonly IEnumerable<IBackgroundTask> _backgroundTasks = backgroundTasks;

    public async ValueTask ExecuteRegisteredJobs(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_backgroundTasks.Select(task => task.ExecuteAsync(cancellationToken).AsTask()));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ExecuteRegisteredJobs(stoppingToken);
    }
}

public interface IBackgroundTask
{
    ValueTask ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class EventCountConfiguration
{
    public int MaxNumber => 1000;
}

public sealed class EvenCount(EventCountConfiguration config) : IBackgroundTask
{
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentCount = 0;

        while (true)
        {
            await Task.Delay(1000, cancellationToken);

            if (currentCount++ >= config.MaxNumber)
            {
                break;
            }

            if (currentCount % 2 == 0)
            {
                Console.WriteLine($"Contando número par: {currentCount}");    
            }
        }
    }
}

public sealed class OddCount(EventCountConfiguration config) : IBackgroundTask
{
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentCount = 0;

        while (true)
        {
            await Task.Delay(1000, cancellationToken);

            if (currentCount++ >= config.MaxNumber)
            {
                break;
            }

            if (currentCount % 2 == 1)
            {
                Console.WriteLine($"Contando número impar: {currentCount}");
            }   
        }
    }
}