using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;
using System;
using System.Globalization;
using System.Threading;


namespace RevitTimasBIMTools.RebarMarkFix
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed partial class AreaRebarMarkFixCommand : IExternalCommand, IExternalCommandAvailability
    {
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                var viewModel = new AreaRebarMarkFixViewModel();
                var window = new AreaRebarMarkFixWindow(viewModel);
                window.Show();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.ToString());
            }

            return Result.Succeeded;
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
