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
    internal sealed partial class AreaRebarMarkFixCommand : IExternalCommand, IExternalCommandAvailability
    {



        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                
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
