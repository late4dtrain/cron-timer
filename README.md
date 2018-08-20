# CronTimer
Simple wrapper around Cronos to have functionality similar to System.Timer

## The use of the library is very similar to the `System.Timer`.
Usage:
```cs
void Main()
{
	var timer = new CronTimer("*/1 * * * * *", true);
	timer.TriggeredEventHander += async (s, e) => await HandleCronTimer(e);
	timer.Start();
	Thread.Sleep(TimeSpan.FromSeconds(7));
	timer.Stop();
}

public Task HandleCronTimer(CronEventArgs e)
{
	if (e.CancellationToken.IsCancellationRequested) 
		return Task.CompletedTask;
	return Task.Run(() => { Console.WriteLine("Task has completed at {0}.", DateTime.UtcNow); });
}
```
