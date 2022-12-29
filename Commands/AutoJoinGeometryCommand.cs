using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;


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
                BoundingBoxIntersectsFilter bbfilter = new(new Outline(bb.Min, bb.Max));
                collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true);
                foreach (Wall wall2 in collector.WherePasses(bbfilter).WherePasses(exfilter).ToElements())
                {
                    if (!JoinGeometryUtils.AreElementsJoined(doc, wall1, wall2))
                    {
                        XYZ normal1 = XYZ.BasisZ.CrossProduct(wall1.Orientation);
                        XYZ normal2 = XYZ.BasisZ.CrossProduct(wall2.Orientation);

                        if (!normal1.IsParallel(normal2)) { continue; }

                        TransactionManager.CreateTransaction(doc, "AutoJoin", () =>
                        {
                            try
                            {
                                JoinGeometryUtils.JoinGeometry(doc, wall1, wall2);
                            }
                            catch { counter--; }
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
        return view is ViewPlan or ViewSchedule or ViewSection or View3D;
    }


    public static string GetPath()
    {
        return typeof(AutoJoinGeometryCommand).FullName;
    }

}
