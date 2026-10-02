using System;
using System.Threading;
using System.Collections.Concurrent;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("Usage: dotnet run -- <threads> <size> <method>"); return; }
        int n = int.Parse(args[0]);
        string m = args[2].ToLower();
        var th = new Thread[n];
        void Pr(int id) => Console.WriteLine($"Thread{id} of {n}: Hello World");

        if (m == "spin")
        {
            int[] cur = { n - 1 };
            for (int i = 0; i < n; i++) { int id = i; th[i] = new Thread(() => {
                while (Volatile.Read(ref cur[0]) != id) { }
                Pr(id); Interlocked.Decrement(ref cur[0]); }); th[i].Start(); }
        }
        else if (m == "cond")
        {
            object mo = new object(); int[] cur = { n - 1 };
            for (int i = 0; i < n; i++) { int id = i; th[i] = new Thread(() => {
                lock (mo) { while (cur[0] != id) Monitor.Wait(mo); Pr(id); cur[0]--; Monitor.PulseAll(mo); } }); th[i].Start(); }
        }
        else if (m == "chan_chain")
        {
            var ch = new BlockingCollection<int>[n];
            for (int i = 0; i < n; i++) ch[i] = new BlockingCollection<int>();
            for (int i = 0; i < n; i++) { int id = i; th[i] = new Thread(() => {
                ch[id].Take(); Pr(id);
                if (id > 0) ch[id - 1].Add(0); }); th[i].Start(); }
            ch[n - 1].Add(0);
        }
        else if (m == "chan_array")
        {
            var per = new SemaphoreSlim[n]; var don = new SemaphoreSlim[n];
            for (int i = 0; i < n; i++) { per[i] = new SemaphoreSlim(0); don[i] = new SemaphoreSlim(0); }
            for (int i = 0; i < n; i++) { int id = i; th[i] = new Thread(() => { per[id].Wait(); Pr(id); don[id].Release(); }); th[i].Start(); }
            for (int id = n - 1; id >= 0; id--) { per[id].Release(); don[id].Wait(); }
        }
        else // wg_chain
        {
            var ev = new ManualResetEventSlim[n + 1];
            for (int i = 0; i <= n; i++) ev[i] = new ManualResetEventSlim(false);
            ev[n].Set();
            for (int i = 0; i < n; i++) { int id = i; th[i] = new Thread(() => { ev[id + 1].Wait(); Pr(id); ev[id].Set(); }); th[i].Start(); }
        }
        foreach (var t in th) t.Join();
    }
}