using System;
using System.Threading;

class Program
{
    // Умножение строк [r0, r1) матрицы C. Порядок циклов i-k-j ради локальности кэша.
    static void Mul(double[] A, double[] B, double[] C, int N, int r0, int r1)
    {
        for (int i = r0; i < r1; i++)
            for (int k = 0; k < N; k++)
            {
                double aik = A[i * N + k];
                for (int j = 0; j < N; j++)
                    C[i * N + j] += aik * B[k * N + j];
            }
    }
    static double Sum(double[] M) { double s = 0; foreach (var x in M) s += x; return s; }

    static void Main(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("Usage: dotnet run -- <threads> <N>"); return; }
        int T = int.Parse(args[0]);
        int N = int.Parse(args[1]);

        var rng = new Random(12345);                 // фикс. seed -> матрицы одинаковые во всех запусках
        double[] A = new double[N * N], B = new double[N * N];
        for (int i = 0; i < N * N; i++) { A[i] = rng.NextDouble(); B[i] = rng.NextDouble(); }

        // --- последовательная эталонная версия ---
        double[] Cs = new double[N * N];
        var w1 = System.Diagnostics.Stopwatch.StartNew();
        Mul(A, B, Cs, N, 0, N);
        w1.Stop();

        // --- параллельная: строки делятся static, последний поток добивает остаток ---
        double[] Cp = new double[N * N];
        int rows = N / T;
        var w2 = System.Diagnostics.Stopwatch.StartNew();
        var th = new Thread[T];
        for (int t = 0; t < T; t++)
        {
            int r0 = t * rows, r1 = (t == T - 1) ? N : r0 + rows;   // копия для замыкания (ловушки нет)
            th[t] = new Thread(() => Mul(A, B, Cp, N, r0, r1));
            th[t].Start();
        }
        foreach (var x in th) x.Join();              // барьер (аналог неявного join в omp parallel)
        w2.Stop();

        double s = w1.Elapsed.TotalMilliseconds, p = w2.Elapsed.TotalMilliseconds;
        Console.WriteLine($"Matrix: {N}x{N}, threads={T}");
        Console.WriteLine($"Sequential time: {s:F4} ms");
        Console.WriteLine($"Parallel time:   {p:F4} ms");
        Console.WriteLine($"Speedup:         {(p > 0 ? s / p : 0):F2}");
        Console.WriteLine($"Results Match:   {Math.Abs(Sum(Cs) - Sum(Cp)) < 1e-6}");
    }
}