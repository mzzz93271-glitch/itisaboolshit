using System.Diagnostics;
using AsyncMonitor;

var service = new CheckService();

var checks = new (string Name, TimeSpan Delay)[]
{
    ("Сеть", TimeSpan.FromMilliseconds(700)),
    ("База данных", TimeSpan.FromMilliseconds(900)),
    ("Файлы", TimeSpan.FromMilliseconds(500))
};

while (true)
{
    Console.WriteLine(
        "\n1 — одна проверка; 2 — последовательно; 3 — параллельно; " +
        "4 — длительная проверка с отменой; 0 — выход");

    var cmd = Console.ReadLine();

    if (cmd == "0")
        break;

    try
    {
        if (cmd == "1")
        {
            var r = await service.CheckServiceAsync(
                checks[0].Name,
                checks[0].Delay);

            Print(new[] { r });
        }
        else if (cmd == "2")
        {
            var sw = Stopwatch.StartNew();
            var r = await service.RunSequentialAsync(checks);
            sw.Stop();

            Console.WriteLine($"Последовательно: {sw.ElapsedMilliseconds} мс");
            Print(r);
        }
        else if (cmd == "3")
        {
            var sw = Stopwatch.StartNew();
            var r = await service.RunParallelAsync(checks);
            sw.Stop();

            Console.WriteLine($"Параллельно: {sw.ElapsedMilliseconds} мс");
            Print(r);
        }
        else if (cmd == "4")
        {
            using var cts = new CancellationTokenSource();

            Console.WriteLine("Запущена длительная проверка. Нажмите Esc для отмены.");

            var checkTask = service.CheckServiceAsync(
                "Длительная проверка",
                TimeSpan.FromSeconds(30),
                cts.Token);

            while (!checkTask.IsCompleted)
            {
                if (Console.KeyAvailable &&
                    Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
                {
                    cts.Cancel();
                    break;
                }

                await Task.Delay(50);
            }

            var result = await checkTask;
            Console.WriteLine("Проверка завершена.");
            Print(new[] { result });
        }
        else
        {
            Console.WriteLine("Неизвестная команда.");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Проверка отменена.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.GetType().Name}: {ex.Message}");
    }
}

static void Print(IEnumerable<CheckResult> xs)
{
    foreach (var result in xs)
    {
        Console.WriteLine(
            $"{result.Name}: {result.Elapsed.TotalMilliseconds:F0} мс");
    }
}