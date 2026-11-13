# CronTimer

CronTimer is a simple and lightweight library for creating cron jobs in .NET. It provides functionality similar to `System.Timer` but uses cron expressions to schedule tasks.

## Features

- Schedule tasks using cron expressions.
- Supports both synchronous and asynchronous operations.
- Integrates with Microsoft.Extensions.Logging for logging.
- Supports dependency injection.

## Installation

You can install the CronTimer library via NuGet:

```shell
dotnet add package Late4dTrain.CronTimer
```

## Usage

### Basic Usage

Here's a basic example of how to use the `CronTimer`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Late4dTrain.CronTimer;
using Late4dTrain.CronTimer.Events;
using Late4dTrain.CronTimer.Parser;

class Program
{
    static async Task Main(string[] args)
    {
        var timer = new CronTimer("*/1 * * * * *", CronFormats.IncludeSeconds);
        timer.TriggeredAsyncEventHandler += async (s, e) => await HandleCronTimer(e);
        timer.Start();

        // Let the timer run for 7 seconds
        await Task.Delay(TimeSpan.FromSeconds(7));
        await timer.StopAsync(CancellationToken.None);
    }

    private static Task HandleCronTimer(CronEventArgs e)
    {
        if (e.CancellationToken.IsCancellationRequested)
            return Task.CompletedTask;

        return Task.Run(() => { Console.WriteLine("Task has completed at {0}.", DateTime.UtcNow); });
    }
}
```

### Using Dependency Injection and Logging

To use `CronTimer` with dependency injection and logging, follow these steps:

1. Add the necessary NuGet packages:

    ```shell
    dotnet add package Microsoft.Extensions.Logging
    dotnet add package Microsoft.Extensions.DependencyInjection
    ```

2. Configure the services in your `Startup` class:

    ```csharp
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Late4dTrain.CronTimer;
    using Late4dTrain.CronTimer.Parser;
    using Late4dTrain.CronTimer.Providers;

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole());

            services.AddSingleton<ITimeProvider, SystemTimeProvider>();
            services.AddSingleton<IDelayProvider, SystemDelayProvider>();

            services.AddTransient<CronTimer>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<CronTimer>>();
                var timeProvider = provider.GetService<ITimeProvider>();
                var delayProvider = provider.GetService<IDelayProvider>();

                return new CronTimer(options =>
                {
                    options.AddCronTabs(new CronTab("*/5 * * * * *", CronFormats.IncludeSeconds));
                }, timeProvider, delayProvider,
                    errorLogger: (message, error) => logger.LogError(error, "{Message}", message),
                    infoLogger: message => logger.LogInformation("{Message}", message));
            });
        }
    }
    ```

3. Use the `CronTimer` in your application:

    ```csharp
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Late4dTrain.CronTimer;
    using Late4dTrain.CronTimer.Events;
    using Late4dTrain.CronTimer.Parser;
    using Late4dTrain.CronTimer.Providers;

    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(configure => configure.AddConsole())
                .AddSingleton<ITimeProvider, SystemTimeProvider>()
                .AddSingleton<IDelayProvider, SystemDelayProvider>()
                .AddTransient<CronTimer>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<CronTimer>>();
                    var timeProvider = provider.GetService<ITimeProvider>();
                    var delayProvider = provider.GetService<IDelayProvider>();

                    return new CronTimer(options =>
                    {
                        options.AddCronTabs(new CronTab("*/5 * * * * *", CronFormats.IncludeSeconds));
                    }, timeProvider, delayProvider,
                        errorLogger: (message, error) => logger.LogError(error, "{Message}", message),
                        infoLogger: message => logger.LogInformation("{Message}", message));
                })
                .BuildServiceProvider();

            var timer = serviceProvider.GetRequiredService<CronTimer>();
            timer.TriggeredAsyncEventHandler += async (s, e) => await HandleCronTimer(e);
            timer.Start();

            // Let the timer run for 7 seconds
            await Task.Delay(TimeSpan.FromSeconds(7));
            await timer.StopAsync(CancellationToken.None);
        }

        private static Task HandleCronTimer(CronEventArgs e)
        {
            if (e.CancellationToken.IsCancellationRequested)
                return Task.CompletedTask;

            return Task.Run(() => { Console.WriteLine("Task has completed at {0}.", DateTime.UtcNow); });
        }
    }
    ```

## Scheduling and callback behavior

- `TriggeredAsyncEventHandler` is awaitable. Its subscribers run one at a time in registration order; an async subscriber must return its `Task` for the timer to track completion and exceptions. `TriggeredEventHandler` remains available for synchronous callbacks.
- `Start()` starts in the background. `StartAsync(token)` returns a task for the worker's lifetime. `Stop()` requests cooperative cancellation; `StopAsync(token)` requests cancellation and awaits worker completion. A canceled stop-wait does not stop waiting work or dispose resources still in use.
- `executionTimeout` cancels the callback token; it does not forcibly terminate code that ignores cancellation. A callback that ignores the token remains awaited, so later occurrences do not overlap it. Use the event's `CancellationToken` in long-running work.
- Cron times are evaluated by `ITimeProvider.UtcNow`. Day-of-month and day-of-week constraints are combined with AND semantics; numeric weekdays use `0` for Sunday through `6` for Saturday. `L` means the month's last day in the day-of-month field; `L-n` means an offset from it; `nW` selects the nearest weekday without crossing the month; `nL` selects that month's last such weekday. A standalone day-of-week `L` matches Saturday. The `#` operator is not implemented.
- If multiple configured schedules have the same next timestamp, each fires once in configuration order. Missed intervals are not replayed after a callback runs late.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more details.

This `README.md` provides an overview of the `CronTimer` library, installation instructions, basic usage examples, and instructions for using dependency injection and logging.
