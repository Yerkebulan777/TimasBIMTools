using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Globalization;
using System.Threading;


namespace RevitTimasBIMTools.RebarConteinerMark
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class AreaRebarMarkFixCommand : IExternalCommand, IExternalCommandAvailability
    {
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            TaskDialog dialog = new("Auto-numbering")
            {
                MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                Title = "Автонумерация позиции",
                TitleAutoPrefix = false,
                AllowCancellation = true,
                MainInstruction = "Для автонумерации позиции могут использоваться номер строки или индекс вложенных семейств",
                MainContent = "Выберите способ автонумерации:"
            };

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Find all rebars");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Select rebar");
            TaskDialogResult dialogResult = dialog.Show();
            //bool result = tdRes switch
            //{
            //    TaskDialogResult.CommandLink1 => false,
            //    TaskDialogResult.CommandLink2 => true,
            //    _ => false
            //};

            return Result.Succeeded;
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
