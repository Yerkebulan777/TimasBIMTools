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


        public static Solid GetSolidByVolume(this Element element, Transform global, Options options, double tolerance = 0.5)
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


        public static Solid GetUnionSolidByVolume(this Element elem, Transform global, Options options, double tolerance = 0.5)
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


        public static Solid GetIntersectionSolid(this Solid source, Element elem, Transform global, Options options, double tolerance = 0)
        {
            Solid result = null;
            GeometryElement geomElement = elem.get_Geometry(options);
            BooleanOperationsType unionType = BooleanOperationsType.Union;
            BooleanOperationsType interType = BooleanOperationsType.Intersect;
            foreach (GeometryObject obj in geomElement.GetTransformed(global))
            {
                if (obj is Solid solid && solid != null && solid.Faces.Size > 0)
                {
                    try
                    {
                        solid = BooleanOperationsUtils.ExecuteBooleanOperation(source, solid, interType);
                        if (result != null && solid != null && solid.Faces.Size > 0)
                        {
                            solid = BooleanOperationsUtils.ExecuteBooleanOperation(result, solid, unionType);
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


        public static BoundingBoxUV GetCountour(this Solid solid, Document doc, Plane plane, SketchPlane sketch, double offset = 0)
        {
            BoundingBoxUV result = null;
            XYZ direction = plane.Normal;
            using (Transaction tx = new(doc, "GetCountour"))
            {
                TransactionStatus status = tx.Start();
                try
                {
                    Face face = ExtrusionAnalyzer.Create(solid, plane, direction).GetExtrusionBase();
                    IList<CurveLoop> curveloops = ExporterIFCUtils.ValidateCurveLoops(face.GetEdgesAsCurveLoops(), direction);
                    result = face.GetBoundingBox();
                    status = tx.Commit();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                    if (!tx.HasEnded())
                    {
                        status = tx.RollBack();
                    }
                }
            }
            return result;
        }


        public static CurveArray ConvertLoopToArray(CurveLoop loop)
        {
            CurveArray a = new();
            foreach (Curve c in loop)
            {
                a.Append(c);
            }
            return a;
        }


        public static Solid ScaledSolidByOffset(this Solid solid, XYZ centroid, BoundingBoxXYZ bbox, double offset, int factor = 3)
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


        public static Solid CreateRectangularPrism(double d1, double d2, double d3)
        {
            List<Curve> profile = new();
            XYZ profile00 = new(-d1 / 2, -d2 / 2, -d3 / 2);
            XYZ profile01 = new(-d1 / 2, d2 / 2, -d3 / 2);
            XYZ profile11 = new(d1 / 2, d2 / 2, -d3 / 2);
            XYZ profile10 = new(d1 / 2, -d2 / 2, -d3 / 2);

            profile.Add(Line.CreateBound(profile00, profile01));
            profile.Add(Line.CreateBound(profile01, profile11));
            profile.Add(Line.CreateBound(profile11, profile10));
            profile.Add(Line.CreateBound(profile10, profile00));

            CurveLoop curveLoop = CurveLoop.Create(profile);

            SolidOptions options = new(ElementId.InvalidElementId, ElementId.InvalidElementId);

            return GeometryCreationUtilities.CreateExtrusionGeometry(new[] { curveLoop }, XYZ.BasisZ, d3, options);
        }


        /// <summary> The dot product of the angle must be greater than cos angle = > cosin /// </summary>
        public static bool IsValidParallel(this XYZ normal, in XYZ direction, in double cosin)
        {
            return Math.Abs(normal.DotProduct(direction)) > cosin;
        }


        public static XYZ ReduceDirection(this XYZ normal)
        {
            double radians = XYZ.Zero.AngleOnPlaneTo(normal, XYZ.BasisZ);
            return radians > Math.PI ? normal : normal.Negate();
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
