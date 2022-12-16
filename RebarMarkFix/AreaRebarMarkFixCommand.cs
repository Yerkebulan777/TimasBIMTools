using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitUtils;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;


namespace RevitTimasBIMTools.RebarMarkFix
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class AreaRebarMarkFixCommand : IExternalCommand, IExternalCommandAvailability
    {
        IDictionary<string, string> valueMap = null;
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
                valueMap = new Dictionary<string, string>(30);
                if (element is AreaReinforcement reinforcement)
                {
                    rebarIds = reinforcement.GetRebarInSystemIds();
                    for (int i = 0; i < rebarIds.Count; i++)
                    {
                        Element elem = doc.GetElement(rebarIds[i]);
                        if (elem is RebarInSystem rebar)
                        {
                            ValidateRebarValueData(rebar);
                            foreach (KeyValuePair<string, string> item in valueMap)
                            {
                                foreach (Parameter param in elem.GetParameters(item.Key))
                                {
                                    param.SetValue(item.Value);
                                }
                            }
                        }
                    }
                }
            }
            return rebarIds;
        }


        void ValidateRebarValueData(RebarInSystem rebar)
        {
            foreach (Parameter param in rebar.GetOrderedParameters())
            {
                if (!param.IsReadOnly && param.UserModifiable)
                {
                    string name = param.Definition.Name;
                    if (!valueMap.ContainsKey(name))
                    {
                        string newValue = param.GetValue();
                        if (!string.IsNullOrEmpty(newValue))
                        {
                            valueMap[name] = newValue;
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
