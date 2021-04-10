using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace NotifyCollector
{
    internal class Collector
    {
        //private static object locker = new object();
        private static ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

        private static Dictionary<string, CollectorKey> keys = new Dictionary<string, CollectorKey>();

        public static void Send(string name, string message, string nameSpace/*, ILogger logger*/, Exception exception = null, long frequency = 0)
        {
            // --- thread safe
            var sendMailKey = CollectorKey.Create(name, nameSpace/*, logger*/, frequency);
            var key = sendMailKey.Key();
            //----------------

            // --- thread safe
            //bool lockTaken = false;
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                //Monitor.Enter(locker, ref lockTaken);

                if (!keys.ContainsKey(key))
                {
                    cacheLock.EnterWriteLock();
                    try
                    {
                        keys[key] = sendMailKey;
                        //logger.Log("Число ключей в кеше: '{0}'", keys.Count);
                        Debug.WriteLine("[{0}] {1} Число ключей в кеше: '{2}'", Thread.CurrentThread.ManagedThreadId, key, keys.Count);
                    }
                    finally
                    {
                        cacheLock.ExitWriteLock();
                    }
                }
                else
                {
                    cacheLock.EnterReadLock();
                    try
                    {
                        sendMailKey = keys[key];
                    }
                    finally
                    {
                        cacheLock.ExitReadLock();
                    }
                    sendMailKey.SetFrequency(frequency);
                }
            }
            catch (Exception ex)
            {
                //logger.LogException("Сборщик уведомлений. Ошибка.", ex);
                Debug.WriteLine("[{0}] Сборщик уведомлений. Ошибка. {1}", Thread.CurrentThread.ManagedThreadId, ex.Message);
            }
            finally
            {
                //if (lockTaken) Monitor.Exit(locker);
                cacheLock.ExitUpgradeableReadLock();
            }
            //----------------

            sendMailKey.Add(message, exception);
            sendMailKey.CheckThread();
        }
    }
}