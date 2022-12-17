using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitUtils;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;


namespace RevitTimasBIMTools.RebarMarkFix
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class AreaRebarMarkFixCommand : IExternalCommand, IExternalCommandAvailability
    {
        IDictionary<string, StringValueData> valueMap = null;
        private sealed class StringValueData
        {
            int stopper { get; set; } = 0;
            internal bool IsValid { get; set; } = false;
            internal string Content { get; set; } = string.Empty;
            IDictionary<string, int> temp { get; set; } = null;

            public StringValueData(string value, int limit)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (temp.TryGetValue(value, out int count))
                    {
                        temp[value] = count++;
                        if (stopper < count)
                        {
                            Content = value;
                            stopper = count;
                            IsValid = true;
                        }
                    }
                    else
                    {
                        temp.Add(value, 0);
                        stopper = limit;
                    }
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
                TaskDialogResult.CommandLink1 => false,
                TaskDialogResult.CommandLink2 => true,
                _ => false
            };

            if (dialogResultBulean)
            {
                IList<ElementId> rebarIds = GetAllAreaRebarIds(doc);
            }

            return Result.Succeeded;
        }


        IList<ElementId> GetAllAreaRebarIds(Document doc)
        {
            IList<ElementId> rebarIds = null;
            foreach (Element element in new FilteredElementCollector(doc).OfClass(typeof(AreaReinforcement)).WhereElementIsElementType())
            {
                valueMap = new Dictionary<string, StringValueData>(30);
                if (element is AreaReinforcement reinforcement)
                {
                    rebarIds = reinforcement.GetRebarInSystemIds();
                    int length = rebarIds.Count;
                    for (int i = 0; i < length; i++)
                    {
                        Element elem = doc.GetElement(rebarIds[i]);
                        if (elem is RebarInSystem rebar)
                        {
                            ValidateRebarValueData(rebar, length);
                            foreach (KeyValuePair<string, StringValueData> item in valueMap)
                            {
                                foreach (Parameter param in elem.GetParameters(item.Key))
                                {
                                    StringValueData value = item.Value;
                                    param.SetValue(value.Content);
                                }
                            }
                        }
                    }
                }
            }
            return rebarIds;
        }



        void ValidateRebarValueData(Element element, int length)
        {
            int limit = length > 100 ? 10 : 5;
            if (element is RebarInSystem rebar)
            {
                IList<Parameter> parameters = rebar.GetOrderedParameters();
                IEnumerator enumerator = parameters.GetEnumerator();
                enumerator.Reset();

                while (enumerator.MoveNext())
                {
                    Parameter param = enumerator.Current as Parameter;
                    if (!param.IsReadOnly && param.UserModifiable)
                    {
                        string name = param.Definition.Name;
                        if (!valueMap.ContainsKey(name))
                        {
                            string value = param.GetValue();
                            StringValueData data = new(value, limit);
                            if (data.IsValid)
                            {
                                valueMap.Add(name, data);
                            }
                        }
                    }
                }
            }
        }


        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            if (uiapp.ActiveUIDocument?.ActiveGraphicalView is View view)
            {
                return view.ViewType is ViewType.FloorPlan or ViewType.ThreeD or ViewType.Section;
            }
            return false;
        }


        public static string GetPath()
        {
            return typeof(AreaRebarMarkFixCommand).FullName;
        }



    }
}
