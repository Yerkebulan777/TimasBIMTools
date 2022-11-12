using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;
using Options = Autodesk.Revit.DB.Options;
using Plane = Autodesk.Revit.DB.Plane;

namespace RevitTimasBIMTools.RevitUtils
{
    internal static class GeometryExtension
    {

        public static Outline GetOutLine(this BoundingBoxXYZ bbox)
        {
            Transform transform = bbox.Transform;
            return new Outline(transform.OfPoint(bbox.Min), transform.OfPoint(bbox.Max));
        }


        public static XYZ GetMiddlePointByBoundingBox(this Element element, out BoundingBoxXYZ bbox)
        {
            bbox = element.get_BoundingBox(null);
            return (bbox.Min + bbox.Max) * 0.5;
        }


        public static XYZ GetHostElementNormal(this Element elem, double tollerance = 0)
        {
            XYZ result = XYZ.BasisZ;
            if (elem is Wall wall)
            {
                result = wall.Orientation.Normalize();
            }
            else if (elem is HostObject hostObject)
            {
                Transform local = Transform.Identity;
                foreach (Reference refFace in HostObjectUtils.GetTopFaces(hostObject))
                {
                    GeometryObject geo = elem.GetGeometryObjectFromReference(refFace);
                    if (geo is Face face && face.Area > tollerance)
                    {
                        try
                        {
                            XYZ normal = result;
                            if (face is PlanarFace planar && planar != null)
                            {
                                normal = planar.FaceNormal;
                            }
                            else
                            {
                                BoundingBoxUV box = face.GetBoundingBox();
                                normal = face.ComputeNormal((box.Max + box.Min) * 0.5);
                                result = local.OfVector(normal).Normalize();
                            }
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
                        {
                            Logger.Error(ex.Message);
                        }
                    }
                }
            }
            return result;
        }


        public static Solid GetSolidByVolume(this Element element, in Transform global, in Options options, double tolerance = 0.5)
        {
            Solid result = null;
            GeometryElement geomElem = element.get_Geometry(options);
            foreach (GeometryObject obj in geomElem.GetTransformed(global))
            {
                if (obj is Solid solid && solid != null)
                {
                    double volume = solid.Volume;
                    if (volume > tolerance)
                    {
                        tolerance = volume;
                        result = solid;
                    }
                }
            }
            return result;
        }


        public static Solid GetUnionSolidByVolume(this Element elem, in Transform global, in Options options, double tolerance = 0.5)
        {
            Solid result = null;
            GeometryElement geomElement = elem.get_Geometry(options);
            BooleanOperationsType unionType = BooleanOperationsType.Union;
            foreach (GeometryObject obj in geomElement.GetTransformed(global))
            {
                if (obj is Solid solid && solid != null && solid.Faces.Size > 0)
                {
                    if (result == null && solid.Volume > tolerance)
                    {
                        result = solid;
                    }
                    else
                    {
                        try
                        {
                            solid = BooleanOperationsUtils.ExecuteBooleanOperation(result, solid, unionType);
                        }
                        finally
                        {
                            if (solid != null && solid.Volume > tolerance)
                            {
                                result = solid;
                            }
                        }
                    }
                }
            }
            return result;
        }


        public static Solid GetIntersectionSolid(this Solid source, in Element elem, in Transform global, in Options options, double tolerance = 0)
        {
            Solid result = null;
            GeometryElement geomElement = elem.get_Geometry(options);
            BooleanOperationsType union = BooleanOperationsType.Union;
            BooleanOperationsType intersect = BooleanOperationsType.Intersect;
            foreach (GeometryObject obj in geomElement.GetTransformed(global))
            {
                if (obj is Solid solid && solid != null && solid.Faces.Size > 0)
                {
                    try
                    {
                        solid = BooleanOperationsUtils.ExecuteBooleanOperation(source, solid, intersect);
                        if (result != null && solid != null && solid.Volume > 0)
                        {
                            solid = BooleanOperationsUtils.ExecuteBooleanOperation(result, solid, union);
                        }
                    }
                    finally
                    {
                        double volume = solid.Volume;
                        if (volume > tolerance)
                        {
                            tolerance = volume;
                            result = solid;
                        }
                    }
                }
            }
            return result;
        }


        public static BoundingBoxUV GetSectionBound(this Solid solid, Document doc, in XYZ direction, in XYZ centroid)
        {
            BoundingBoxUV result = null;
            using (Transaction tx = new(doc, "GetSectionBound"))
            {
                TransactionStatus status = tx.Start();
                try
                {
                    Plane plane = Plane.CreateByNormalAndOrigin(direction, centroid);
                    Face face = ExtrusionAnalyzer.Create(solid, plane, direction).GetExtrusionBase();
                    IList<CurveLoop> curveloops = ExporterIFCUtils.ValidateCurveLoops(face.GetEdgesAsCurveLoops(), direction);
                    result = face.GetBoundingBox();
                    status = tx.Commit();
                }
                catch (Exception ex)
                {
                    if (!tx.HasEnded())
                    {
                        status = tx.RollBack();
                        Logger.Error(ex.ToString());
                    }
                }
            }
            return result;
        }


        public static Solid ScaledSolidByOffset(this Solid solid, in XYZ centroid, in BoundingBoxXYZ bbox, double offset, int factor = 3)
        {
            XYZ minPnt = bbox.Min;
            XYZ maxPnt = bbox.Max;
            XYZ pnt = new(offset, offset, offset);
            double minDiagonal = minPnt.DistanceTo(maxPnt);
            double maxDiagonal = (minPnt - (pnt * factor)).DistanceTo(maxPnt + (pnt * factor));
            Transform trans = Transform.CreateTranslation(XYZ.Zero).ScaleBasisAndOrigin(maxDiagonal / minDiagonal);
            solid = SolidUtils.CreateTransformed(solid, trans.Multiply(Transform.CreateTranslation(centroid).Inverse));
            return SolidUtils.CreateTransformed(solid, Transform.CreateTranslation(centroid));
        }


        public static int GetIntersectingLinkedElementIds(this Solid solid, IList<RevitLinkInstance> links, List<ElementId> ids)
        {
            int count = ids.Count;
            foreach (RevitLinkInstance lnk in links)
            {
                Transform transform = lnk.GetTransform();
                if (!transform.AlmostEqual(Transform.Identity))
                {
                    solid = SolidUtils.CreateTransformed(solid, transform.Inverse);
                }
                ElementIntersectsSolidFilter filter = new(solid);
                FilteredElementCollector intersecting = new FilteredElementCollector(lnk.GetLinkDocument()).OfClass(typeof(FamilyInstance)).WherePasses(filter);
                ids.AddRange(intersecting.ToElementIds());
            }
            return ids.Count - count;
        }


        public static Solid Sphere(this XYZ center, double radius = 0.75)
        {
            Frame frame = new(center, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ);

            XYZ XyzOnArc = center + (radius * XYZ.BasisX);
            XYZ start = center - (radius * XYZ.BasisZ);
            XYZ end = center + (radius * XYZ.BasisZ);

            Arc arc = Arc.Create(start, end, XyzOnArc);

            Line line = Line.CreateBound(arc.GetEndPoint(1), arc.GetEndPoint(0));

            CurveLoop halfCircle = new();
            halfCircle.Append(arc);
            halfCircle.Append(line);

            List<CurveLoop> loops = new(1)
            {
                halfCircle
            };

            return GeometryCreationUtilities.CreateRevolvedGeometry(frame, loops, 0, 2 * Math.PI);
        }


        public static void CreateDirectShape(this Solid solid, Document doc, BuiltInCategory builtIn = BuiltInCategory.OST_GenericModel)
        {
            try
            {
                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(builtIn));
                ds.ApplicationDataId = doc.ProjectInformation.UniqueId;
                ds.SetShape(new GeometryObject[] { solid });
                ds.Name = "DirectShapeBySolid";
            }
            catch (Exception exc)
            {
                Logger.Error(exc.Message);
            }
        }


        /// <summary> The dot product of the angle must be greater than cos angle = > cosin /// </summary>
        public static bool IsValidParallel(this XYZ normal, XYZ direction, double cosin)
        {
            return Math.Abs(normal.DotProduct(direction)) > cosin;
        }


        public static double GetHorizontAngleByNormal(this XYZ normal)
        {
            return Math.Atan(normal.X / normal.Y);
        }


        public static double GetVerticalAngleBetween(this XYZ normal, in XYZ direction)
        {
            double normalAngle = normal.DotProduct(XYZ.BasisZ);
            double directAngle = direction.DotProduct(XYZ.BasisZ);
            return Math.Abs(Math.Acos(Math.Round(normalAngle - directAngle, 5)));
        }


        public static double GetHorizontAngleBetween(this XYZ normal, in XYZ direction)
        {
            double normalAngle = Math.Atan2(normal.Y, normal.X);
            double directAngle = Math.Atan2(direction.Y, direction.X);
            double angle = Math.Abs(Math.Round(normalAngle - directAngle, 5));
            angle = angle > Math.PI ? (Math.PI * 2) - angle : angle;
            angle = angle > (Math.PI / 2) ? Math.PI - angle : angle;
            return angle;
        }


        public static bool IsParallel(this XYZ normal, in XYZ direction)
        {
            return normal.CrossProduct(direction).IsZeroLength();
        }


        public static double ConvertToDegrees(this double radians, int digit = 3)
        {
            return Math.Round(180 / Math.PI * radians, digit);
        }

    }
}
