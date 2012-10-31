using System;
using System.Diagnostics;
using System.IO;

namespace easySync
{
    /// <summary>
    /// Logger class
    /// </summary>
    public static class Log
    {
        private const String LogError = "unable to dump object (probably, public object property thrown an exception)";
        private const String ExceptionMessageFormat = "{0} {1}";
        private readonly static String FileCreationErrorFormat = "Error writing to application log!" + Environment.NewLine + "{0}";

        private static String fileName = null;
        private static object fileLock = new object();

        static Log()
        {
            try
            {
                String logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Consts.Application.ProfileFolder);

                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                fileName = Path.Combine(logFolder, Consts.Application.LogFileName);
            }
            catch (Exception ex)
            {
                ErrorHelper.ShowErrorBox(String.Format(FileCreationErrorFormat, ex.Message), ex, true);
            }
        }

        /// <summary>
        /// Write entry to log
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void Write(String message)
        {
            writeLog(message);
        }

        /// <summary>
        /// Write entry to log
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="obj">Object to describe</param>
        public static void Write(String message, object obj)
        {
            writeLog(message);
            try
            {
                ObjectDescriptor descriptor = new ObjectDescriptor(obj);
                writeLog(descriptor.ToString());
            }
            catch (Exception e)
            {
                writeLog(LogError);
                writeLog(describeException(e));
            }
        }

        /// <summary>
        /// Write entry to log
        /// </summary>
        /// <param name="ex">Exception to describe</param>
        public static void Write(Exception ex, int depth = 1)
        {
            StackTrace sTrace = new StackTrace(true);
            writeLog(String.Format(Consts.Log.MethodGotExceptionFormat, sTrace.GetFrame(depth).GetMethod().DeclaringType.Name,
                sTrace.GetFrame(depth).GetMethod().Name, describeException(ex)));
        }

        /// <summary>
        /// Write entry to log
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="ex">Exception to describe</param>
        public static void Write(String message, Exception ex)
        {
            writeLog(String.Format(ExceptionMessageFormat, message, describeException(ex)));
        }

        /// <summary>
        /// Calling method name
        /// </summary>
        /// <returns>Method name</returns>
        public static String selfName()
        {
            StackTrace sTrace = new StackTrace(true);
            return String.Format(Consts.Log.MethodFormat, sTrace.GetFrame(1).GetMethod().DeclaringType.Name,
                sTrace.GetFrame(1).GetMethod().Name);
        }

        /// <summary>
        /// Returns described exception
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns>Information about exception</returns>
        private static String describeException(Exception ex)
        {
            return String.Format(Consts.Log.ExceptionFullFormat, ex.GetType().Name, ex.Message,
                ex.TargetSite.ReflectedType.FullName, ex.TargetSite.Name, ex.StackTrace);
        }

        /// <summary>
        /// Write entry to log file
        /// </summary>
        /// <param name="message">Message to log</param>
        private static void writeLog(String message)
        {
            lock (fileLock)
            {
                try
                {
                    File.AppendAllText(fileName, String.Format(Consts.Log.LogMessageFormat, DateTime.Now, message));
                    File.AppendAllText(fileName, Environment.NewLine);
                }
                catch (Exception ex)
                {
                    ErrorHelper.ShowErrorBox(String.Format(FileCreationErrorFormat, ex.Message), ex, true);
                }
            }
        }
    }
}
