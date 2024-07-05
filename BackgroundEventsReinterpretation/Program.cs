using System.Reflection;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;

await UseHangfire();

return; 

async ValueTask UseMsExtHosting()
{
    IHost host = new HostBuilder()
        .ConfigureServices(services =>
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

async ValueTask UseHangfire()
{
    await using MsSqlContainer sqlServer = new MsSqlBuilder()
        .Build();
    await sqlServer.StartAsync();

    IHost host = new HostBuilder()
        .ConfigureServices((hostOptions, services) =>
        {
            services.AddHangfire(configuration => configuration
                    // ReSharper disable once AccessToDisposedClosure
                    .UseSqlServerStorage(sqlServer.GetConnectionString()))
                .AddHangfireServer()
                .AddSingleton<IBackgroundTaskListener, HangfireTaskListener>()
                .AddHostedService<HangfireTaskListener>()
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

public sealed class HangfireTaskListener(
    IBackgroundJobClient hangfireJobClient, 
    IEnumerable<IBackgroundTask> backgroundTasks) 
    : BackgroundService, IBackgroundTaskListener
{
    readonly IBackgroundJobClient _hangfireJobClient = hangfireJobClient;
    readonly IEnumerable<IBackgroundTask> _backgroundTasks = backgroundTasks;

    public async ValueTask ExecuteRegisteredJobs(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_backgroundTasks.Select(task =>
        {
            Type jobType = task.GetType();
            MethodInfo? jobMethod = jobType.GetMethod(nameof(IBackgroundTask.ExecuteAsync));
            Job hangFireJob = new(jobType, jobMethod, cancellationToken);

            _hangfireJobClient.Create(hangFireJob, new EnqueuedState(EnqueuedState.DefaultQueue));

            return Task.CompletedTask;
        }));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ExecuteRegisteredJobs(stoppingToken);
    }
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
    string Identifier { get; }

    ValueTask ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class EventCountConfiguration
{
    public int MaxNumber => 1000;
}

public sealed class EvenCount(EventCountConfiguration config) : IBackgroundTask
{
    public string Identifier => "BackgroundEvenCount";

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
    public string Identifier => "BackgroundOddCount";

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