using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;


namespace RevitTimasBIMTools.Commands
{
    internal sealed class AutoJoinGeometryCommand : IExternalCommand, IExternalCommandAvailability
    {
        private FilteredElementCollector collector { get; set; }

        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            double tollerance = 50 * 304.8;
            BuiltInCategory bip = BuiltInCategory.OST_Walls;
            collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true);
            foreach (Wall wall in collector)
            {
                if (wall.FindInserts(true, true, true, true).Any())
                {
                    BoundingBoxXYZ bb = wall.get_BoundingBox(null);
                    WallType wallType = doc.GetElement(wall.GetTypeId()) as WallType;
                    if (wallType is not null && wallType.Width < tollerance) { continue; }
                    BoundingBoxIntersectsFilter bbfilter = new(new Outline(bb.Min, bb.Max));
                    collector ??= RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true);
                    foreach (Element itm in collector.WherePasses(bbfilter))
                    {
                        if (!JoinGeometryUtils.IsCuttingElementInJoin(doc, wall, itm))
                        {
                            try
                            {
                                JoinGeometryUtils.JoinGeometry(doc, wall, itm);
                            }
                            finally
                            {

                            }
                        }
                    }
                }
            }

            return Result.Succeeded;
        }


        [STAThread]
        bool IExternalCommandAvailability.IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            View view = uiapp.ActiveUIDocument?.ActiveGraphicalView;
            return view is ViewPlan or ViewSchedule or ViewSection or View3D;
        }


        public static string GetPath()
        {
            return typeof(AutoJoinGeometryCommand).FullName;
        }
    }
}
