# CronTimer
Simple timer wrapper around Cronos.

## The use of the library is very similar to the `System.Timer`.
Usage:
```cs
void Main()
{
	var timer = new CronTimer(cronExpression: "*/5 * * * * *", 
						includeSeconds: true);
	timer.TriggeredEventHander += async (s, e) => await HandleCronTimer();
	timer.Start();
}

public Task HandleCronTimer() {
	return Task.Run(() => { Console.WriteLine("It is {0} and all is well", DateTime.UtcNow); });
}
```
