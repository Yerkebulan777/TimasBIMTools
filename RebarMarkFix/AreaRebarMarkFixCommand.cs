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
            _ = uidoc.Document;

            try
            {
                AreaRebarMarkFixViewModel viewModel = new();
                AreaRebarMarkFixWindow window = new(viewModel);
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
            if (uiapp.ActiveUIDocument?.ActiveGraphicalView is View view)
            {
                if (view.ViewType == ViewType.FloorPlan)
                {
                    return true;
                }
            }
            return false;
        }


        public static string GetPath()
        {
            return typeof(AreaRebarMarkFixCommand).FullName;
        }


    }
}
