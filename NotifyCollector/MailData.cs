using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyCollector
{
    /// <summary>
    /// Данные о уведомлении
    /// </summary>
    internal class MailData
    {
        private long ticks;
        private string message;
        private Exception exception;

        /// <summary>
        /// Ctor
        /// </summary>
        internal MailData(long ticks, string message, Exception exception = null)
        {
            this.ticks = ticks;
            this.message = message;
            this.exception = exception;
        }

        /// <summary>
        /// Копировать в новый экземпляр
        /// </summary>
        internal static MailData Copy(MailData mailData)
        {
            return new MailData(mailData.ticks, mailData.message, mailData.exception);
        }

        public override string ToString()
        {
            string body;
            if (exception != null)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("{0} - уведомление:", new DateTime(ticks).ToString("G")).AppendLine();
                sb.Append(message).AppendLine();
                sb.AppendFormat("Ошибка: {0}", exception.Message).AppendLine();
                sb.AppendFormat("{0}", exception.StackTrace).AppendLine();
                body = string.Format("{0}", sb.ToString());
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendFormat("{0} - уведомление:", new DateTime(ticks).ToString("G")).AppendLine();
                sb.Append(message).AppendLine();
                body = string.Format("{0}", sb.ToString());
            }
            return body;
        }
    }
}
