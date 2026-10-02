using System;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("Usage: dotnet run -- <threads> <N> <type> [chunk]"); return; }
        int T = int.Parse(args[0]);
        int N = int.Parse(args[1]);
        string type = args[2].ToLower();
        int chunk = args.Length > 3 ? int.Parse(args[3]) : 1;
        if (type == "runtime")
        {
            string env = Environment.GetEnvironmentVariable("LAB2_SCHEDULE") ?? "static";
            var p = env.Split(',');
            type = p[0].Trim().ToLower();
            if (p.Length > 1) chunk = int.Parse(p[1].Trim());
            Console.WriteLine($"[runtime] LAB2_SCHEDULE={env} -> type={type}, chunk={chunk}");
        }
        var a = new double[N]; var b = new double[N];
        for (int i = 0; i < N; i++) a[i] = i;
        b[0] = a[0]; b[N - 1] = a[N - 1];
        int end = N - 2, next = 1;
        var lk = new object();
        var iters = new int[T]; var blocks = new int[T];
        void Proc(int s, int e) { for (int i = s; i <= e; i++) b[i] = (a[i - 1] + a[i] + a[i + 1]) / 3.0; }
        void Work(int t)
        {
            if (type == "static")
            {
                if (chunk <= 1)
                {
                    int inner = end, bs = inner / T, rem = inner % T;
                    int s = 1 + t * bs + Math.Min(t, rem);
                    int cnt = bs + (t < rem ? 1 : 0);
                    if (cnt > 0) { Proc(s, s + cnt - 1); iters[t] = cnt; blocks[t] = 1; }
                }
                else
                    for (long q = 1L + (long)t * chunk; q <= end; q += (long)T * chunk)
                    { int e = (int)Math.Min(q + chunk - 1, end); Proc((int)q, e); iters[t] += e - (int)q + 1; blocks[t]++; }
            }
            else
                while (true)
                {
                    int s = 0, e = 0; bool got = false;
                    lock (lk)
                    {
                        if (next <= end)
                        {
                            int sz = type == "guided" ? Math.Max(chunk, (end - next + 1) / T) : chunk;
                            if (sz < 1) sz = 1;
                            s = next; e = Math.Min(s + sz - 1, end); next = e + 1; got = true;
                        }
                    }
                    if (!got) break;
                    Proc(s, e); iters[t] += e - s + 1; blocks[t]++;
                }
        }
        var th = new Thread[T];
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int t = 0; t < T; t++) { int id = t; th[t] = new Thread(() => Work(id)); th[t].Start(); }
        foreach (var x in th) x.Join();
        sw.Stop();
        double sum = 0; for (int i = 0; i < N; i++) sum += b[i];
        double exp = (double)N * (N - 1) / 2.0;
        Console.WriteLine($"Schedule: {type}{(chunk > 1 ? " " + chunk : "")}, threads={T}, N={N}");
        Console.WriteLine($"Time: {sw.Elapsed.TotalMilliseconds:F4} ms");
        Console.WriteLine($"Checksum: {sum:F0} expected: {exp:F0} OK: {Math.Abs(sum - exp) < 1e-6}");
        for (int t = 0; t < T; t++) Console.WriteLine($"  Thread{t}: iters={iters[t]}, blocks={blocks[t]}");
    }
}