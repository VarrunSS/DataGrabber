using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataGrabber.LogWriter
{
    public class Logger
    {
        private static string m_exePath = string.Empty;
        public static string LogFileName = string.Empty;


        private static EventWaitHandle _waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, "SHARED_BY_ALL_PROCESSES");


        public Logger(string logMessage)
        {
            Write(logMessage);
        }

        public static void Write(string logMessage, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            m_exePath = Path.Combine(
                ConfigurationManager.AppSettings["ApplicationBasePath"].ToString(),
                ConfigurationManager.AppSettings["LogPath"].ToString());

            if (!Directory.Exists(m_exePath))
            {
                Directory.CreateDirectory(m_exePath);
            }
            LogFileName = m_exePath + "Log_" + DateTime.Now.ToString("yyyyMMdd").ToString() + ".txt";

            try
            {
                logMessage = $"{logMessage}; Line no: {lineNumber}; Method: {caller}";
                using (StreamWriter w = File.AppendText(LogFileName))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {

                txtWriter.Write("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("  : {0}", logMessage);
                txtWriter.Close();

            }
            catch (Exception ex)
            {
            }
        }

        public static void WriteTest(string logMessage)
        {
            m_exePath = Path.Combine(
                 ConfigurationManager.AppSettings["ApplicationBasePath"].ToString(), 
                 ConfigurationManager.AppSettings["LogPath"].ToString());

            if (!Directory.Exists(m_exePath))
            {
                Directory.CreateDirectory(m_exePath);
            }
            LogFileName = m_exePath + "LogData_" + DateTime.Now.ToString("yyyyMMdd").ToString() + ".txt";

            try
            {
                _waitHandle.WaitOne();

                /* process file*/
                using (StreamWriter w = File.AppendText(LogFileName))
                {
                    Log(logMessage, w);
                }

                _waitHandle.Set();
            }
            catch (Exception ex)
            {
            }
        }


    }
}
