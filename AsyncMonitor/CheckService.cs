using System.Diagnostics;

namespace AsyncMonitor;

public sealed record CheckResult(string Name, TimeSpan Elapsed);

public sealed class CheckService
{
    public async Task<CheckResult> CheckServiceAsync(
        string name,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя проверки не может быть пустым.", nameof(name));

        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), "Длительность не может быть отрицательной.");

        var sw = Stopwatch.StartNew();

        await Task.Delay(delay, cancellationToken);

        sw.Stop();
        return new CheckResult(name, sw.Elapsed);
    }

    public async Task<IReadOnlyList<CheckResult>> RunSequentialAsync(
        IReadOnlyList<(string Name, TimeSpan Delay)> checks,
        CancellationToken token = default)
    {
        var results = new List<CheckResult>();

        foreach (var (name, delay) in checks)
        {
            results.Add(await CheckServiceAsync(name, delay, token));
        }

        return results;
    }

    public async Task<IReadOnlyList<CheckResult>> RunParallelAsync(
        IReadOnlyList<(string Name, TimeSpan Delay)> checks,
        CancellationToken token = default)
    {
        var tasks = checks
            .Select(check => CheckServiceAsync(check.Name, check.Delay, token))
            .ToArray();

        return await Task.WhenAll(tasks);
    }
}