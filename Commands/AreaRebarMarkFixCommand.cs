using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.ViewModels;
using RevitTimasBIMTools.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;


namespace RevitTimasBIMTools.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed partial class AreaRebarMarkFixCommand : IExternalCommand, IExternalCommandAvailability
    {
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
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
            View view = uiapp.ActiveUIDocument?.ActiveGraphicalView;
            if (view is ViewPlan or ViewSchedule or ViewSection or View3D)
            {
                IList catList = new List<int>
                {
                    (int)BuiltInCategory.OST_Walls,
                    (int)BuiltInCategory.OST_Floors,
                    (int)BuiltInCategory.OST_AreaRein
                };
                foreach (Category cat in selectedCategories)
                {
                    if (catList.Contains(cat.Id.IntegerValue))
                    {
                        return true;
                    }
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
