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
        XYZ offset = new(0.1, 0.1, 0.1);
        FilteredElementCollector collector;
        BuiltInCategory bip = BuiltInCategory.OST_Walls;
        IList<Element> allWalls = RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true).ToElements();
        foreach (Wall wall1 in allWalls)
        {
            if (wall1.FindInserts(true, true, true, true).Any())
            {
                BoundingBoxXYZ bb = wall1.get_BoundingBox(null);
                ElementLevelFilter level1Filter = new(wall1.LevelId);
                Outline outline = new Outline(bb.Min -= offset, bb.Max += offset);
                collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true);
                collector = collector.WherePasses(new BoundingBoxIntersectsFilter(outline));
                foreach (Wall wall2 in collector.WherePasses(level1Filter).ToElements())
                {
                    if (JoinGeometryUtils.AreElementsJoined(doc, wall1, wall2)) { continue; }

                    Line line1 = (wall1.Location as LocationCurve).Curve as Line;
                    Line line2 = (wall2.Location as LocationCurve).Curve as Line;

                    XYZ normal1 = line1.Direction.ToPositive();
                    XYZ normal2 = line2.Direction.ToPositive();

                    if (normal1.IsAlmostEqualTo(normal2))
                    {
                        using Transaction trx = new(doc);
                        TransactionStatus status = trx.Start("JoinWall");
                        if (status == TransactionStatus.Started)
                        {
                            try
                            {
                                XYZ point1 = line1.Evaluate(0.5, false);
                                XYZ point2 = line2.Evaluate(0.5, false);
                                double distance = point1.DistanceTo(point2);
                                double maximum = (line1.Length + line2.Length) / 3;
                                if (distance != 0 && distance < maximum)
                                {
                                    JoinGeometryUtils.JoinGeometry(doc, wall1, wall2);
                                    status = trx.Commit();
                                    counter++;
                                }
                            }
                            catch (Exception ex)
                            {
                                SBTLogger.Log(ex.Message);
                            }
                            finally
                            {
                                if (!trx.HasEnded())
                                {
                                    status = trx.RollBack();
                                    trx.Dispose();
                                }
                            }
                        }
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
