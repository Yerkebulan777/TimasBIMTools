﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;


namespace RevitTimasBIMTools.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class AutoJoinGeometryCommand : IExternalCommand, IExternalCommandAvailability
    {
        private FilteredElementCollector collector { get; set; }
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            int counter = 0;
            BuiltInCategory bip = BuiltInCategory.OST_Walls;
            collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true);
            ICollection<ElementId> exclusionIds = new List<ElementId>(collector.GetElementCount());
            TransactionManager.CreateTransaction(doc, "AutoJoin", () => 
            {
                foreach (Wall wall in collector)
                {
                    if (wall.FindInserts(true, true, true, true).Any())
                    {
                        exclusionIds.Add(wall.Id);
                        BoundingBoxXYZ bb = wall.get_BoundingBox(null);
                        ExclusionFilter exfilter = new ExclusionFilter(exclusionIds);
                        BoundingBoxIntersectsFilter bbfilter = new(new Outline(bb.Min, bb.Max));
                        collector ??= RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true);
                        foreach (Element itm in collector.WherePasses(bbfilter).WherePasses(exfilter))
                        {
                            if (!JoinGeometryUtils.IsCuttingElementInJoin(doc, wall, itm))
                            {
                                try
                                {
                                    JoinGeometryUtils.JoinGeometry(doc, wall, itm);
                                }
                                finally
                                {
                                    counter++;
                                }
                            }
                        }
                    }
                }
            });

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
}
