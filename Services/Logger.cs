﻿using Autodesk.Revit.DB;
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
    public class Logger
    {
        private static ILog mainlogger;
        private const string caption = "Timas BIM Tools";
        private static readonly StringBuilder sb = new StringBuilder();
        private static readonly string documentPath = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        private static readonly string logFilePath = Path.Combine(documentPath, "TimasBIMToolLog", "Revit.log");
        public static void InitMainLogger(Type type)
        {
            string name = type.ToString();
            log4net.Repository.ILoggerRepository repository = log4net.LogManager.CreateRepository(name);
            mainlogger = log4net.LogManager.GetLogger(name, type);
            RollingFileAppender LogFile = new RollingFileAppender
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
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);
            return sf.GetMethod().Name;
        }

        public static void Log(Exception ex)
        {
            mainlogger?.Error("Error", ex);
            sb.AppendLine($"\n{ex.Message}");
            Debug.WriteLine($"\n{ex.Message}");
            File.AppendAllText(logFilePath, sb.ToString());
            sb.Clear();
        }

        public static void Log(string text, Exception ex)
        {
            mainlogger?.Error(text, ex);
            sb.AppendLine($"\n{text}\n{ex.Message}");
            Debug.WriteLine($"\n{text}\n{ex.Message}");
            File.AppendAllText(logFilePath, sb.ToString());
            sb.Clear();
        }

        public static void Log(string text)
        {
            sb.AppendLine(text);
            Debug.WriteLine(text);
            mainlogger?.Info(text);
            File.AppendAllText(logFilePath, sb.ToString());
            sb.Clear();
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
