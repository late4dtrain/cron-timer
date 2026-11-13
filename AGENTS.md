# Repository guidance

- This is a small .NET solution: `src/Late4dTrain.CronTimer` is the `netstandard2.1` library; `src/Late4dTrain.CronTimer.Tests` is the `net8.0` xUnit test project.
- Scheduling time and delay are abstracted behind `ITimeProvider` and `IDelayProvider`; use the controllable `MockTimeProvider` and `MockDelayProvider` in `src/Late4dTrain.CronTimer.Tests/Providers` rather than real-time scheduling waits.
- Verify changes in CI order: `dotnet restore`, `dotnet build`, then `dotnet test --no-build --no-restore --verbosity normal --collect "XPlat Code Coverage" --results-directory TestResults --logger trx`. Run focused tests with `dotnet test src/Late4dTrain.CronTimer.Tests --filter FullyQualifiedName~TestName`.
- When asked to create a plan, save it as Markdown under `.agents/plans/feat/` (create the directory if needed); do not leave the plan only in chat.
- Project-specific read-only review agents are in `.opencode/agents/`: `code-review-uncommitted` reviews working-tree changes; `code-review-branch` reviews the current branch against its base.
