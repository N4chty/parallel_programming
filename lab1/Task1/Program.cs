using System;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Использование: dotnet run -- <число_потоков>");
            return;
        }
        int numThreads = int.Parse(args[0]);

        object printLock = new object();     
        Thread[] threads = new Thread[numThreads];

        for (int i = 0; i < numThreads; i++)
        {
            int id = i;                      
            threads[i] = new Thread(() =>
            {
                lock (printLock)              
                {
                    Console.WriteLine($"Thread{id} of {numThreads}: Hello World");
                }
            });
            threads[i].Start();               
        }

        foreach (var t in threads) t.Join();  
    }
}