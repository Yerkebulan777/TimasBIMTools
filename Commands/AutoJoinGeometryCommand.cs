using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;

namespace RevitTimasBIMTools.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal sealed class AutoJoinGeometryCommand : IExternalCommand, IExternalCommandAvailability
{

    Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication uiapp = commandData.Application;
        UIDocument uidoc = uiapp.ActiveUIDocument;
        Document doc = uidoc.Document;

        int counter = 0;
        FilteredElementCollector collector;
        BuiltInCategory bip = BuiltInCategory.OST_Walls;
        IList<Element> allWalls = RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true).ToElements();
        ICollection<ElementId> exclusionIds = new List<ElementId>(allWalls.Count);
        foreach (Wall wall1 in allWalls)
        {
            if (wall1.FindInserts(true, true, true, true).Any())
            {
                exclusionIds.Add(wall1.Id);
                ExclusionFilter exfilter = new(exclusionIds);
                BoundingBoxXYZ bb = wall1.get_BoundingBox(null);
                ElementLevelFilter level1Filter = new(wall1.LevelId);
                BoundingBoxIntersectsFilter bbfilter = new(new Outline(bb.Min, bb.Max));
                collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true);
                foreach (Wall wall2 in collector.WherePasses(bbfilter).WherePasses(level1Filter).WherePasses(exfilter))
                {
                    if (JoinGeometryUtils.AreElementsJoined(doc, wall1, wall2)) { continue; }

                    Line line1 = (wall1.Location as LocationCurve).Curve as Line;
                    Line line2 = (wall2.Location as LocationCurve).Curve as Line;

                    XYZ normal1 = line1.Direction.Normalize().ToPositive();
                    XYZ normal2 = line2.Direction.Normalize().ToPositive();

                    if (normal1.IsAlmostEqualTo(normal2) && !line1.IsCollinear(line2))
                    {
                        uidoc.Selection.SetElementIds(new List<ElementId>() {wall2.Id});
                        TransactionManager.CreateTransaction(doc, "AutoJoin", () =>
                        {
                            try
                            {
                                JoinGeometryUtils.JoinGeometry(doc, wall1, wall2);
                            }
                            finally { counter++; }
                        });
                    }
                }
            }
        }

        SBTLogger.Info($"Successfully Completed!\n Joined walls: {counter} count");
        return Result.Succeeded;
    }


    [STAThread]
    bool IExternalCommandAvailability.IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
    {
        View view = uiapp.ActiveUIDocument?.ActiveGraphicalView;
        return view is ViewPlan or View3D;
    }


    public static string GetPath()
    {
        return typeof(AutoJoinGeometryCommand).FullName;
    }

}
