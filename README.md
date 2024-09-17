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

class Program
{
    static async Task Main(string[] args)
    {
        var timer = new CronTimer("*/1 * * * * *", CronFormats.IncludeSeconds);
        timer.TriggeredEventHandler += async (s, e) => await HandleCronTimer(e);
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
                }, timeProvider, delayProvider, logger);
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
                    }, timeProvider, delayProvider, logger);
                })
                .BuildServiceProvider();

            var timer = serviceProvider.GetRequiredService<CronTimer>();
            timer.TriggeredEventHandler += async (s, e) => await HandleCronTimer(e);
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

## License

This project is licensed under the MIT License. See the `LICENSE` file for more details.

This `README.md` provides an overview of the `CronTimer` library, installation instructions, basic usage examples, and instructions for using dependency injection and logging.
