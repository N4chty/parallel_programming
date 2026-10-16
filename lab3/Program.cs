using System;
using System.IO;
using System.Threading;
using System.Globalization;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("Usage: dotnet run -- <threads> <N> <loop|func>"); return; }
        int T = int.Parse(args[0]);
        int N = int.Parse(args[1]);
        string ver = args[2].ToLower();
        var inv = CultureInfo.InvariantCulture;

        // --- генерируем файл точек детерминированно (seed фикс.) и ЧИТАЕМ его обратно ---
        // Так соблюдаем и условие "читать из файла", и требование "размер задачи аргументом".
        // InvariantCulture обязателен: иначе на русской Windows числа запишутся с запятой.
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

        // --- последовательный эталон для проверки корректности ---
        double ex = 0, ey = 0, ez = 0;
        for (int i = 0; i < N; i++) { ex += xs[i]; ey += ys[i]; ez += zs[i]; }

        // --- параллельный счёт: две версии декомпозиции ---
        double[] sx = new double[T], sy = new double[T], sz = new double[T];
        var th = new Thread[T];
        if (ver == "loop")                       // распараллеливание цикла: точки режутся по потокам
        {
            int bs = N / T, rem = N % T;
            for (int t = 0; t < T; t++)
            {
                int id = t, s = id * bs + Math.Min(id, rem), cnt = bs + (id < rem ? 1 : 0);
                th[id] = new Thread(() => {
                    double lx = 0, ly = 0, lz = 0;
                    for (int i = s; i < s + cnt; i++) { lx += xs[i]; ly += ys[i]; lz += zs[i]; }
                    sx[id] = lx; sy[id] = ly; sz[id] = lz;
                });
            }
        }
        else                                     // функциональная декомпозиция: ветви = оси x,y,z
        {
            for (int t = 0; t < T; t++)
            {
                int id = t;
                th[id] = new Thread(() => {
                    double lx = 0, ly = 0, lz = 0;
                    for (int a = 0; a < 3; a++) if (a % T == id)   // ось a достается потоку id
                    {
                        if (a == 0) for (int i = 0; i < N; i++) lx += xs[i];
                        else if (a == 1) for (int i = 0; i < N; i++) ly += ys[i];
                        else for (int i = 0; i < N; i++) lz += zs[i];
                    }
                    sx[id] = lx; sy[id] = ly; sz[id] = lz;
                });
            }
        }
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int t = 0; t < T; t++) th[t].Start();
        for (int t = 0; t < T; t++) th[t].Join();
        sw.Stop();

        double px = 0, py = 0, pz = 0;
        for (int t = 0; t < T; t++) { px += sx[t]; py += sy[t]; pz += sz[t]; }
        double cx = px / N, cy = py / N, cz = pz / N;
        bool ok = Math.Abs(cx - ex / N) < 1e-9 && Math.Abs(cy - ey / N) < 1e-9 && Math.Abs(cz - ez / N) < 1e-9;

        Console.WriteLine($"Version: {ver}, threads={T}, N={N}");
        Console.WriteLine($"Center: ({cx:F6}, {cy:F6}, {cz:F6})");
        Console.WriteLine($"Expected: ({ex / N:F6}, {ey / N:F6}, {ez / N:F6})");
        Console.WriteLine($"OK: {ok}");
        Console.WriteLine($"Parallel time: {sw.Elapsed.TotalMilliseconds:F4} ms");
    }
}