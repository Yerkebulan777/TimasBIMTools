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
            internal int Counter { get; set; } = 0;
            internal string Content { get; set; } = string.Empty;
            private IDictionary<string, int> data { get; set; } = new Dictionary<string, int>();

            public StringValueData(string value)
            {
                data.Add(value, 0);
            }


            internal void SetNewValue(string value)
            {
                if (data.TryGetValue(value, out int count))
                {
                    data[value] = count++;
                    if (Counter < count)
                    {
                        Content = value;
                        Counter = count;
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


        private void RetrievAreaRebarParameters(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(AreaReinforcement));
            foreach (Element item in collector.WhereElementIsNotElementType())
            {
                if (item is AreaReinforcement reinforcement)
                {
                    map = new Dictionary<string, StringValueData>();
                    IList<ElementId> rebarIds = reinforcement.GetRebarInSystemIds();
                    ElementId rebarId = rebarIds.FirstOrDefault(rid => rid.IntegerValue > 0);
                    IList<Parameter> parameters = GetStringParameters(doc, rebarId);
                    while (0 < rebarIds.Count)
                    {
                        int num = rnd.Next(0, rebarIds.Count);
                        Element elem = doc.GetElement(rebarIds[num]);
                        if (elem is RebarInSystem rebar)
                        {
                            if (ValidateParameters(rebar, parameters))
                            {
                                rebarIds.Remove(rebarIds[num]);
                            }
                        }
                    }
                }
            }
        }


        private bool ValidateParameters(RebarInSystem rebar, IList<Parameter> parameters, int limit = 3)
        {
            bool allValid = map.Values.All(s => s.Counter > limit);
            bool allFilled = parameters.Count == map.Count;
            for (int i = 0; i < parameters.Count; i++)
            {
                Parameter param = parameters[i];
                string value = param.GetValue();
                string name = param.Definition.Name;
                if (allFilled && allValid && string.IsNullOrEmpty(value))
                {
                    if (map.TryGetValue(name, out StringValueData result))
                    {
                        rebar.get_Parameter(param.GUID).SetValue(result.Content);
                    }
                }
                else if (!map.TryGetValue(name, out StringValueData data))
                {
                    map.Add(name, new StringValueData(value));
                }
                else if (data != null)
                {
                    data.SetNewValue(value);
                }
            }
            return allFilled && allValid;
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
                        if (param.StorageType is not StorageType.String || !param.IsShared || param.IsReadOnly)
                        {
                            if (parameters.Remove(param)) { continue; }
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
