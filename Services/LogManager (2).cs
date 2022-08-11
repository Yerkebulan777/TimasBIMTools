using Autodesk.Revit.UI;
using log4net;
using log4net.Appender;
using log4net.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace RevitTimasBIMTools.Services
{
    public static class LogManager
    {
        private static ILog mainlogger;
        private const string caption = "Timas BIM Tools";
        public static void InitMainLogger(Type type)
        {
            string name = type.ToString();
            log4net.Repository.ILoggerRepository repository = log4net.LogManager.CreateRepository(name);
            mainlogger = log4net.LogManager.GetLogger(name, type);
            string documentPath = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            string LogFilePath = Path.Combine(documentPath, "TimasBIMToolLog", "Revit.log");
            RollingFileAppender LogFile = new RollingFileAppender
            {
                File = LogFilePath,
                MaxSizeRollBackups = 10,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                DatePattern = "_dd-MM-yyyy",
                MaximumFileSize = "10MB"
            };
            LogFile.ActivateOptions();
            LogFile.AppendToFile = true;
            LogFile.Encoding = Encoding.UTF8;
            LogFile.Layout = new log4net.Layout.XmlLayoutSchemaLog4j();
            LogFile.ActivateOptions();
            BasicConfigurator.Configure(repository, LogFile);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string GetCurrentMethod()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);
            return sf.GetMethod().Name;
        }

        public static void Log(Exception ex)
        {
            Debug.WriteLine($"\n{ex.Message}");
            mainlogger?.Error("Error", ex);
        }

        public static void Log(string text, Exception ex)
        {
            Debug.WriteLine($"\n{text}\n{ex.Message}");
            mainlogger?.Error(text, ex);
        }

        public static void Log(string text)
        {
            Debug.WriteLine(text);
            mainlogger?.Info(text);
        }

        public static void Error(string text)
        {
            mainlogger?.Error(text);
            string intro = "Error: ";
            Debug.WriteLine($"\n{intro}\t{text}");
            TaskDialog dlg = new TaskDialog(caption)
            {
                MainContent = text,
                MainInstruction = intro,
                MainIcon = TaskDialogIcon.TaskDialogIconInformation
            };
            try
            {
                dlg.Show();
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }
        }


        public static void Warning(string text)
        {
            mainlogger?.Warn(text);
            string intro = "Warning: ";
            Debug.WriteLine($"\n{intro}\t{text}");
            TaskDialog dlg = new TaskDialog(caption)
            {
                MainContent = text,
                MainInstruction = intro,
                MainIcon = TaskDialogIcon.TaskDialogIconInformation
            };
            try
            {
                dlg.Show();
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }
        }


        public static void Info(string text)
        {
            mainlogger?.Info(text);
            string intro = "Information: ";
            Debug.WriteLine($"\n{intro}\t{text}");
            TaskDialog dlg = new TaskDialog(caption)
            {
                MainContent = text,
                MainInstruction = intro,
                MainIcon = TaskDialogIcon.TaskDialogIconInformation
            };
            try
            {
                dlg.Show();
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }
        }
    }
}
