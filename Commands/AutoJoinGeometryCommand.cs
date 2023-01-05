using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;


namespace RevitTimasBIMTools.Commands;


[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal sealed class AutoJoinGeometryCommand : IExternalCommand, IExternalCommandAvailability
{

    public const int MaxWitdhInMm = 50;

    Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication uiapp = commandData.Application;
        UIDocument uidoc = uiapp.ActiveUIDocument;
        Level level = uidoc.ActiveView.GenLevel;
        Document doc = uidoc.Document;

        if (level is null || !level.IsValidObject)
        {
            message = "Level not Valid";
            return Result.Failed;
        }

        int counter = 0;
        XYZ offset = new(0.1, 0.1, 0.1);
        FilteredElementCollector collector;
        BuiltInCategory bip = BuiltInCategory.OST_Walls;
        ElementLevelFilter level1Filter = new(level.Id);
        double nativeWitdh = UnitUtils.ConvertToInternalUnits(MaxWitdhInMm, DisplayUnitType.DUT_MILLIMETERS);
        collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true);
        foreach (Wall walltrg in collector.WherePasses(level1Filter).ToElements())
        {
            if (walltrg.FindInserts(true, true, true, true).Any())
            {
                BoundingBoxXYZ bb = walltrg.get_BoundingBox(null);
                Outline outline = new(bb.Min -= offset, bb.Max += offset);
                collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true);
                collector = collector.WherePasses(new BoundingBoxIntersectsFilter(outline));
                foreach (Wall wallsrs in collector.WherePasses(level1Filter).ToElements())
                {
                    if (JoinGeometryUtils.AreElementsJoined(doc, walltrg, wallsrs)) { continue; }

                    WallType wallType = doc.GetElement(wallsrs.GetTypeId()) as WallType;

                    if (nativeWitdh < wallType.Width) { continue; }

                    Line lineTrg = (walltrg.Location as LocationCurve).Curve as Line;
                    Line lineSrs = (wallsrs.Location as LocationCurve).Curve as Line;

                    XYZ normal1 = lineTrg.Direction.ToPositive();
                    XYZ normal2 = lineSrs.Direction.ToPositive();

                    if (normal1.IsAlmostEqualTo(normal2))
                    {
                        using Transaction trx = new(doc);
                        TransactionStatus status = trx.Start("JoinWall");
                        if (status == TransactionStatus.Started)
                        {
                            try
                            {
                                JoinGeometryUtils.JoinGeometry(doc, walltrg, wallsrs);
                                status = trx.Commit();
                                counter++;
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
        return view is ViewPlan;
    }


    public static string GetPath()
    {
        return typeof(AutoJoinGeometryCommand).FullName;
    }

}
