using Autodesk.Revit.DB;
using Autodesk.Revit.UI;


namespace RevitTimasBIMTools.RebarConteinerMark
{
    internal sealed class RebarConteinerMarkCommand : IExternalCommand
    {
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

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
            TaskDialogResult tdRes = dialog.Show();
            var result = tdRes switch
            {
                TaskDialogResult.CommandLink1 => false,
                TaskDialogResult.CommandLink2 => true,
                _ => false
            };

            return Result.Succeeded;
        }


        public static string GetPath()
        {
            return typeof(RebarConteinerMarkCommand).FullName;
        }
    }
}
