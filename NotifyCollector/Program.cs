using System;
using System.Diagnostics;
using System.Threading;

namespace NotifyCollector
{
    class Program
    {
        static void Main(string[] args)
        {
            var threadingTest1 = new ThreadingTest("dddd", "ssssss");
            var threadingTest2 = new ThreadingTest("wwww", "tttttt");
            var threadingTest3 = new ThreadingTest("xxxx", "vvvvvv");
            var threadingTest4 = new ThreadingTest("yyyy", "kkkkkk");
            var threadingTest5 = new ThreadingTest("qqqq", "mmmmmm");

            var manualReset = new ManualResetEventSlim();

            threadingTest1.Start(manualReset);
            threadingTest2.Start(manualReset);
            threadingTest3.Start(manualReset);
            threadingTest4.Start(manualReset);
            threadingTest5.Start(manualReset);

            Thread.Sleep(100);

            manualReset.Set();

            Thread.Sleep(10*1000);

            threadingTest1.Cancel();
            threadingTest2.Cancel();
            threadingTest3.Cancel();
            threadingTest4.Cancel();
            threadingTest5.Cancel();

            Console.ReadLine();
        }
    }

    internal class ThreadingTest
    {
        private string v1;
        private string v2;
        private bool cancel;

        internal ThreadingTest(string v1, string v2)
        {
            this.v1 = v1;
            this.v2 = v2;
            cancel = false;
        }

        internal void Start(ManualResetEventSlim manualReset)
        {
            for (int i = 0; i < 2; i++)
            {
                var thread = new Thread(Execute);
                thread.IsBackground = true;
                thread.Start(manualReset);
                Debug.WriteLine("Запущен поток [{0}] - {1}{2}", thread.ManagedThreadId, v1, v2);
            }
        }

        internal void Cancel()
        {
            cancel = true;
        }

        private void Execute(object @event)
        {
            ((ManualResetEventSlim)@event).Wait();

            while (!cancel)
            {
                try
                {
                    Collector.Send(v1, "message1", v2, frequency: TimeSpan.FromSeconds(10).Ticks);
                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[{0}] {1}{2} Program send message. Ошибка. {3}", Thread.CurrentThread.ManagedThreadId, v1, v2, ex.Message);
                }
            }
        }
    }
}