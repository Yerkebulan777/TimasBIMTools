using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using log4net;
using log4net.Appender;
using log4net.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace RevitTimasBIMTools.Services
{
    public sealed class Logger
    {
        private static ILog mainlogger;
        private const string caption = "Timas BIM Tools";
        private static readonly string documentPath = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        private static readonly string logFilePath = Path.Combine(documentPath, "TimasBIMToolLog", "Revit.log");
        public static void InitMainLogger(Type type)
        {
            string name = type.ToString();
            log4net.Repository.ILoggerRepository repository = log4net.LogManager.CreateRepository(name);
            mainlogger = log4net.LogManager.GetLogger(name, type);
            RollingFileAppender LogFile = new()
            {
                File = logFilePath,
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
            StackTrace st = new();
            StackFrame sf = st.GetFrame(1);
            return sf.GetMethod().Name;
        }


        public static void ThreadProcessLog(string name)
        {
            Thread th = Thread.CurrentThread;
            Debug.WriteLine($"Task Thread ID: {th.ManagedThreadId}, Thread Name: {th.Name}, Process Name: {name}");
        }


        public static void Log(Exception ex)
        {
            mainlogger?.Error("Error", ex);
            Debug.WriteLine($"\n{ex.Message}");
        }

        public static void Log(string text)
        {
            Debug.WriteLine(text);
            mainlogger?.Info(text);
        }

        public static void Log(string text, Exception ex)
        {
            mainlogger?.Error(text, ex);
            Debug.WriteLine($"\n{text}\n{ex.Message}");
        }

        public static void Error(string text)
        {
            mainlogger?.Error(text);
            string intro = "Error: ";
            Debug.WriteLine($"\n{intro}\t{text}");
            TaskDialog dlg = new(caption)
            {
                MainContent = text,
                MainInstruction = intro,
                MainIcon = TaskDialogIcon.TaskDialogIconInformation
            };
            try
            {
                _ = dlg.Show();
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }
        }


        public static void Error(int intId, string text)
        {
            mainlogger?.Error(text);
            string intro = "Error: ";
            Debug.WriteLine($"\n{intro}\t{text}");
            System.Windows.Clipboard.SetText(intId.ToString());
            TaskDialog dlg = new(caption)
            {
                MainContent = text,
                MainInstruction = intro,
                MainIcon = TaskDialogIcon.TaskDialogIconInformation
            };
            try
            {
                _ = dlg.Show();
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
            TaskDialog dlg = new(caption)
            {
                MainContent = text,
                MainInstruction = intro,
                MainIcon = TaskDialogIcon.TaskDialogIconInformation
            };
            try
            {
                _ = dlg.Show();
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
            TaskDialog dlg = new(caption)
            {
                MainContent = text,
                MainInstruction = intro,
                MainIcon = TaskDialogIcon.TaskDialogIconInformation
            };
            try
            {
                _ = dlg.Show();
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }
        }

        public static string ElementDescription(Element elem)
        {
            if (elem.IsValidObject && elem is FamilyInstance)
            {
                FamilyInstance finst = elem as FamilyInstance;

                string typeName = elem.GetType().Name;
                string famName = null == finst ? string.Empty : $"{finst.Symbol.Family.Name}";
                string catName = null == elem.Category ? string.Empty : $"{elem.Category.Name}";
                string symbName = null == finst || elem.Name.Equals(finst.Symbol.Name) ? string.Empty : $"{finst.Symbol.Name}";

                return $"{famName}-{symbName}<{elem.Id.IntegerValue} {elem.Name}>({typeName}-{catName})";
            }
            return "<null>";
        }
    }
}
