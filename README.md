# NotifyCollector
Принимает уведомления в виде текста. Уведомления аккумулируются в каналах. Канал определяется ключом в виде строки. Под капотом Dictionary<,>. Маршрутизации нет. Выполняет некоторую операцию (к примеру отправка письма) по критерию. Критерий - истечение некоторого заданного времени от времени последнего уведомления в канале.

Thread.Abort vs CancellationToken

```
namespace CancelationToken
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var cts = new CancellationTokenSource())
            {
                var thread = new Thread(Callback);
                Console.WriteLine("Запуск дочернего потока");
                thread.Start(cts.Token); // Запускаем поток, передав ему токен отмены

                Thread.Sleep(1000); // Имитация работы основного потока
                Console.WriteLine("Посылаем сигнал дочернему потоку об отмене операции");
                cts.Cancel(); // Посылаем сигнал дочернему потоку что ему необходимо завершить работу
                thread.Join(); // И дожидаемся завершения потока
                Console.WriteLine("Дочерний поток завершен");
            }

            Console.ReadLine();
        }

        private static void Callback(object state)
        {
            var token = (CancellationToken)state;

            // Бесконечный цикл, имитирующий какую-либо длительную работу.
            while (true)
            {
                // Если поступает сигнал отмены, то завершить работу потока
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Работа потока была досрочно завершена");
                    return;
                }

                // Иначе, работает дальше
                Thread.Sleep(1000);
            }
        }
    }
}
```
