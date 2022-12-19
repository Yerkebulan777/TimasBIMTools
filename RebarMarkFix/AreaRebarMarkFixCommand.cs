using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;


namespace RevitTimasBIMTools.RebarMarkFix
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class AreaRebarMarkFixCommand : IExternalCommand, IExternalCommandAvailability
    {
        private readonly Random rnd = new();
        private IDictionary<string, StringValueData> map = new Dictionary<string, StringValueData>();


        private sealed class StringValueData
        {
            private int stopper { get; set; } = 0;
            internal bool IsValid { get; private set; } = false;
            internal string Content { get; private set; } = string.Empty;


            private readonly IDictionary<string, int> data = new Dictionary<string, int>();

            public StringValueData(string value, int limit)
            {
                data.Add(value, 0);
                stopper = limit;
            }


            internal void SetNewValue(string value)
            {
                if (data.TryGetValue(value, out int count))
                {
                    data[value] = count++;
                    if (stopper < count)
                    {
                        Content = value;
                        stopper = count;
                        IsValid = true;
                    }
                }
                else
                {
                    data.Add(value, 0);
                }
            }


        }


        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            TaskDialog dialog = new("Fix area reinforcement mark")
            {
                Title = "Fix of area reinforcement mark",
                MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                MainContent = "Choose a method:",
                AllowCancellation = true,
                TitleAutoPrefix = false,
            };

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Find all rebars");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Select rebar");

            TaskDialogResult dialogResult = dialog.Show();
            bool dialogResultBulean = dialogResult switch
            {
                TaskDialogResult.CommandLink1 => true,
                TaskDialogResult.CommandLink2 => false,
                _ => false
            };

            if (dialogResultBulean)
            {
                RetrievAreaRebarParameters(doc);
            }

            return Result.Succeeded;
        }


        private void RetrievAreaRebarParameters(Document doc, int percentage = 15)
        {

            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(AreaReinforcement));
            foreach (Element item in collector.WhereElementIsNotElementType())
            {
                if (item is AreaReinforcement reinforcement)
                {
                    map = new Dictionary<string, StringValueData>(percentage);
                    IList<ElementId> rebarIds = reinforcement.GetRebarInSystemIds();
                    ElementId rebarId = rebarIds.FirstOrDefault(rid => rid.IntegerValue > 0);
                    IList<Parameter> parameters = GetStringParameters(doc, rebarId);
                    int limit = percentage / 100 * rebarIds.Count;
                    bool AllFilled = false;
                    while (0 < rebarIds.Count)
                    {
                        int counts = parameters.Count;
                        AllFilled = counts == map.Count;
                        int num = rnd.Next(0, rebarIds.Count);
                        Element elem = doc.GetElement(rebarIds[num]);
                        if (elem is RebarInSystem rebar)
                        {
                            for (int i = 0; i < counts; i++)
                            {
                                Parameter param = parameters[i];
                                string value = param.GetValue();
                                string name = param.Definition.Name;
                                if (AllFilled && map.Values.All(s => s.IsValid))
                                {
                                    if (string.IsNullOrWhiteSpace(value))
                                    {

                                    }
                                }
                                else if (map.TryGetValue(name, out StringValueData data))
                                {
                                    data.SetNewValue(value);
                                }
                                else if (!string.IsNullOrEmpty(value))
                                {
                                    map.Add(name, new StringValueData(value, limit));
                                }
                            }
                        }
                    }
                }
            }
        }


        private IList<Parameter> GetStringParameters(Document doc, ElementId rebarId)
        {
            IList<Parameter> parameters = null;
            if (rebarId is not null and ElementId)
            {
                Element element = doc.GetElement(rebarId);
                if (element is RebarInSystem rebar)
                {
                    parameters = rebar.GetOrderedParameters();
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        Parameter param = parameters[i];
                        StorageType storage = param.StorageType;
                        if (param.IsReadOnly || storage is not StorageType.String)
                        {
                            if (parameters.Remove(param)) { continue; }
                        }
                        else if (!param.UserModifiable)
                        {
                            _ = parameters.Remove(param);
                        }
                    }
                }
            }
            return parameters;
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            return uiapp.ActiveUIDocument?.ActiveGraphicalView is View view && view.ViewType is ViewType.FloorPlan;
        }


        public static string GetPath()
        {
            return typeof(AreaRebarMarkFixCommand).FullName;
        }



    }
}
