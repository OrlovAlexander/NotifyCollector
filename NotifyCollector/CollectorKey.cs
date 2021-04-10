using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotifyCollector
{
    /// <summary>
    /// Собирает сообщения и выполняет отправку Уведомления через некоторое время после прихода последнего уведомления с таким же ключом.
    /// <see cref="CollectorKey.Execute"/>
    /// </summary>
    internal class CollectorKey : IDisposable
    {
        //private object locker = new object();
        private ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        private object threadLocker = new object();

        private long lastSend;
        private long frequency;
        private Thread thread;
        private string name;
        private string nameSpace;
        //private ILogger logger;

        private List<MailData> mailDatas;

        /// <summary>
        /// Ctor
        /// </summary>
        private CollectorKey()
        {
            mailDatas = new List<MailData>();
        }

        internal static CollectorKey Create(string name, string nameSpace/*, ILogger logger*/, long frequency = 0)
        {
            // --- thread safe
            var sendMailKey = new CollectorKey();
            sendMailKey.name = name;
            sendMailKey.nameSpace = nameSpace;
            //sendMailKey.logger = logger;
            sendMailKey.frequency = frequency <= 0 ? TimeSpan.FromMinutes(30).Ticks : frequency;

            return sendMailKey;
            // --------------
        }

        internal void Add(string message, Exception exception = null)
        {
            // --- thread safe
            //bool lockTaken = false;
            cacheLock.EnterWriteLock();
            try
            {
                //Monitor.Enter(locker, ref lockTaken);

                lastSend = DateTime.Now.Ticks;
                mailDatas.Add(new MailData(lastSend, message, exception));
            }
            finally
            {
                //if (lockTaken) Monitor.Exit(locker);
                cacheLock.ExitWriteLock();
            }
            // --------------

            //logger.Log("Сборщик уведомлений. Число уведомлений в коллекторе: '{0}'", mailDatas.Count);
            Debug.WriteLine("[{0}] {1} Сборщик уведомлений. Add. Число уведомлений в коллекторе: '{2}'", Thread.CurrentThread.ManagedThreadId, Key(), mailDatas.Count);
        }

        internal string Key()
        {
            // --- thread safe
            cacheLock.EnterReadLock();
            try
            {
                var internalName = name;
                var internalNameSpace = nameSpace;
                return internalName + internalNameSpace;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
            // --------------
        }

        internal void CheckThread()
        {
            //logger.Log("thread - CheckThread");
            Debug.WriteLine("[{0}] {1} thread - CheckThread", Thread.CurrentThread.ManagedThreadId, Key());

            // --- thread safe
            bool lockTaken = false;
            try
            {
                Monitor.Enter(threadLocker, ref lockTaken);

                if (!IsThreadRunning())
                {
                    thread = null;
                    thread = new Thread(Execute);
                    thread.IsBackground = true;
                    thread.Start();
                }
            }
            finally
            {
                if (lockTaken) Monitor.Exit(threadLocker);
            }
            // --------------

            Debug.WriteLine("[{0}] {1} Поток для отслеживания загрузки - [{2}]", Thread.CurrentThread.ManagedThreadId, Key(), thread.ManagedThreadId);
        }

        internal void SetFrequency(long frequency)
        {
            // --- thread safe
            //bool lockTaken = false;
            cacheLock.EnterWriteLock();
            try
            {
                //Monitor.Enter(locker, ref lockTaken);

                this.frequency = frequency <= 0 ? TimeSpan.FromMinutes(30).Ticks : frequency;
            }
            finally
            {
                //if (lockTaken) Monitor.Exit(locker);
                cacheLock.ExitWriteLock();
            }
            // --------------
        }

        private void Execute()
        {
            // --- thread safe
            var key = Key();
            var internalLastSend = GetLastSendThreadSafe();
            var internalFrequency = GetFrequencyThreadSafe();
            // --------------

            //logger.Log("Сборщик уведомлений. Execute. '{0}', '{1}', '{2}'", key, internalLastSend, internalFrequency);
            Debug.WriteLine("[{0}] {1} Сборщик уведомлений. Execute. '{2}', '{3}', '{4}'", Thread.CurrentThread.ManagedThreadId, key, key, internalLastSend, internalFrequency);

            //Thread.Sleep(new TimeSpan(internalFrequency));
            while (DateTime.Now.Ticks - internalLastSend < internalFrequency)
            {
                Thread.Sleep(10);

                // --- thread safe
                internalLastSend = GetLastSendThreadSafe();
                internalFrequency = GetFrequencyThreadSafe();
                // --------------
            }

            var count = 0;
            List<MailData> mailDataCopy = null;

            // --- thread safe
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                count = mailDatas.Count();
                mailDataCopy = new List<MailData>(count);
                foreach (var data in mailDatas)
                {
                    mailDataCopy.Add(MailData.Copy(data));
                }
                cacheLock.EnterWriteLock();
                try
                {
                    mailDatas.Clear();
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
            // --------------

            //logger.Log("Сборщик уведомлений. Execute. Число элементов для отправки '{0}'. Завершение. '{1}'", count, key);
            Debug.WriteLine("[{0}] {1} Сборщик уведомлений. Execute. Число элементов для отправки '{2}'. Завершение. '{3}'", Thread.CurrentThread.ManagedThreadId, key, count, key);
            Console.WriteLine("[{0}] {1} Сборщик уведомлений. Execute. Число элементов для отправки '{2}'. Завершение. '{3}'", Thread.CurrentThread.ManagedThreadId, key, count, key);

            if (mailDataCopy != null && mailDataCopy.Count > 0)
            {
                var sendMail = new SendMail();
                try
                {
                    sendMail.Send(name, nameSpace/*, logger*/, new List<MailData>(mailDataCopy));
                }
                catch (Exception ex)
                {
                    //logger.LogException("Сборщик уведомлений. Execute. Ошибка.", ex);
                    Debug.WriteLine("[{0}] {1} Сборщик уведомлений. Execute. Ошибка. {2}", Thread.CurrentThread.ManagedThreadId, key, ex.Message);

                    // Вернуть копию обратно
                    cacheLock.EnterWriteLock();
                    try
                    {
                        mailDatas.InsertRange(0, mailDataCopy);
                    }
                    finally
                    {
                        cacheLock.ExitWriteLock();
                    }
                }

            }
        }

        private long GetLastSendThreadSafe()
        {
            // --- thread safe
            //bool lockTakenInIf = false;
            cacheLock.EnterReadLock();
            try
            {
                //Monitor.Enter(locker, ref lockTakenInIf);

                var internalLastSend = lastSend;
                return internalLastSend;
            }
            finally
            {
                //if (lockTakenInIf) Monitor.Exit(locker);
                cacheLock.ExitReadLock();
            }
            // --------------
        }

        internal long GetFrequencyThreadSafe()
        {
            // --- thread safe
            //bool lockTakenInIf = false;
            cacheLock.EnterReadLock();
            try
            {
                //Monitor.Enter(locker, ref lockTakenInIf);

                var internalFrequency = frequency;
                return internalFrequency;
            }
            finally
            {
                //if (lockTakenInIf) Monitor.Exit(locker);
                cacheLock.ExitReadLock();
            }
            // --------------
        }

        // Метод в обоих случаях вызывается под замком, поэтому можно не защищать
        private bool IsThreadRunning()
        {
            if (thread == null)
            {
                //logger.Log("thread - IsThreadRunning - thread == null");
                Debug.WriteLine("[{0}] {1} thread - IsThreadRunning - thread == null", Thread.CurrentThread.ManagedThreadId, Key());

                return false;
            }
            if ((thread.ThreadState & System.Threading.ThreadState.Background) == System.Threading.ThreadState.Background || (thread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) == System.Threading.ThreadState.WaitSleepJoin)
            {
                return true;
            }
            //logger.Log("thread - IsThreadRunning - thread.ThreadState != ThreadState.Running. State: '{0}'", thread.ThreadState);
            Debug.WriteLine("[{0}] {1} thread - IsThreadRunning - thread.ThreadState != ThreadState.Running. State: '{2}'", Thread.CurrentThread.ManagedThreadId, Key(), thread.ThreadState);

            return false;
        }

        public void Dispose()
        {
            //logger.Log("thread - Dispose");
            Debug.WriteLine("[{0}] {1} thread - Dispose", Thread.CurrentThread.ManagedThreadId, Key());

            // --- thread safe
            bool lockTaken = false;
            try
            {
                Monitor.Enter(threadLocker, ref lockTaken);

                if (IsThreadRunning())
                {
                    thread.Abort();
                    thread.Join(30);
                    thread = null;
                }
            }
            finally
            {
                if (lockTaken) Monitor.Exit(threadLocker);
            }
            // --------------
        }
    }
}
