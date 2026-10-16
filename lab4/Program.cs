using System;
using System.IO;
using System.Threading;
using System.Globalization;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("Usage: dotnet run -- <threads> <N>"); return; }
        int T = int.Parse(args[0]);
        int N = int.Parse(args[1]);
        var inv = CultureInfo.InvariantCulture;

        // детерминированно генерируем файл точек (seed фикс.) и ЧИТАЕМ его обратно
        string path = "points.txt";
        var rng = new Random(42);
        using (var w = new StreamWriter(path))
            for (int i = 0; i < N; i++)
            {
                double x = rng.NextDouble(), y = rng.NextDouble(), z = rng.NextDouble();
                w.Write(x.ToString(inv)); w.Write(' '); w.Write(y.ToString(inv)); w.Write(' '); w.WriteLine(z.ToString(inv));
            }
        double[] xs = new double[N], ys = new double[N], zs = new double[N];
        var lines = File.ReadAllLines(path);
        for (int i = 0; i < N; i++)
        {
            var p = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            xs[i] = double.Parse(p[0], inv); ys[i] = double.Parse(p[1], inv); zs[i] = double.Parse(p[2], inv);
        }

        // последовательный эталон скалярного среднего (Σx+Σy+Σz)/3N
        double ex = 0, ey = 0, ez = 0;
        for (int i = 0; i < N; i++) { ex += xs[i]; ey += ys[i]; ez += zs[i]; }
        double expected = (ex + ey + ez) / (3.0 * N);

        // функциональная декомпозиция: 3 ветви (оси x,y,z) обновляют ГЛОБАЛЬНУЮ сумму
        // через КРИТИЧЕСКУЮ СЕКЦИЮ (lock) — без неё была бы гонка на global
        double global = 0;
        object globalLock = new object();
        var th = new Thread[T];
        for (int t = 0; t < T; t++)
        {
            int id = t;
            th[id] = new Thread(() =>
            {
                for (int a = 0; a < 3; a++) if (a % T == id)   // ось a достается потоку id
                {
                    double ls = 0;
                    if (a == 0) for (int i = 0; i < N; i++) ls += xs[i];
                    else if (a == 1) for (int i = 0; i < N; i++) ls += ys[i];
                    else for (int i = 0; i < N; i++) ls += zs[i];
                    lock (globalLock) { global += ls; }          // критическая секция
                }
            });
        }
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int t = 0; t < T; t++) th[t].Start();
        for (int t = 0; t < T; t++) th[t].Join();               // неявный барьер
        sw.Stop();

        double result = global / (3.0 * N);
        bool ok = Math.Abs(result - expected) < 1e-9;
        Console.WriteLine($"threads={T}, N={N}");
        Console.WriteLine($"Result:   {result:F9}");
        Console.WriteLine($"Expected: {expected:F9}");
        Console.WriteLine($"OK: {ok}");
        Console.WriteLine($"Parallel time: {sw.Elapsed.TotalMilliseconds:F4} ms");
    }
}