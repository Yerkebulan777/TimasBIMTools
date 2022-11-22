using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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


        public static XYZ GetHostPositiveNormal(this Element elem, double tollerance = 0)
        {
            XYZ resultNormal = XYZ.BasisZ;
            if (elem is Wall wall)
            {
                resultNormal = wall.Orientation.Normalize();
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
                            XYZ normal = resultNormal;
                            if (face is PlanarFace planar && planar != null)
                            {
                                normal = planar.FaceNormal;
                            }
                            else
                            {
                                BoundingBoxUV box = face.GetBoundingBox();
                                normal = face.ComputeNormal((box.Max + box.Min) * 0.5);
                                resultNormal = local.OfVector(normal).Normalize();
                            }
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
                        {
                            Logger.Error(ex.Message);
                        }
                    }
                }
            }
            return resultNormal.ConvertToPositive();
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


        public static List<XYZ> GetIntersectionVerticles(this Solid source, in Element elem, in Transform global, in Options options)
        {
            List<XYZ> vertices = new(3);
            GeometryElement geomElement = elem.get_Geometry(options);
            BooleanOperationsType intersect = BooleanOperationsType.Intersect;
            foreach (GeometryObject obj in geomElement.GetTransformed(global))
            {
                if (obj is Solid solid && solid != null && solid.Faces.Size > 0)
                {
                    try
                    {
                        solid = BooleanOperationsUtils.ExecuteBooleanOperation(source, solid, intersect);
                    }
                    finally
                    {
                        foreach (Face f in solid.Faces)
                        {
                            Mesh mesh = f.Triangulate();
                            int n = mesh.NumTriangles;

                            for (int i = 0; i < n; ++i)
                            {
                                MeshTriangle triangle = mesh.get_Triangle(i);

                                XYZ p1 = triangle.get_Vertex(0);
                                XYZ p2 = triangle.get_Vertex(1);
                                XYZ p3 = triangle.get_Vertex(2);

                                vertices.Add(p1);
                                vertices.Add(p2);
                                vertices.Add(p3);
                            }
                        }
                    }
                }
            }
            return vertices;
        }


        public static double SignedDistanceTo(this Plane plane, XYZ pnt)
        {
            Debug.Assert(plane.Normal.GetLength() == 1, "expected normalised plane normal");
            return plane.Normal.DotProduct(pnt - plane.Origin);
        }


        /// <summary> Project given 3D XYZ point onto plane. </summary>
        public static XYZ ProjectOnto(this Plane plane, XYZ p)
        {
            double d = plane.SignedDistanceTo(p);

            XYZ q = p + d * plane.Normal;

            return q;
        }


        public static BoundingBoxUV GetSectionBound(this Solid solid, Document doc, in XYZ normal, in XYZ centroid, out IList<CurveLoop> loops)
        {
            loops = null;
            BoundingBoxUV result = null;
            using (Transaction trx = new(doc, "GetSectionBound"))
            {
                TransactionStatus status = trx.Start();
                try
                {
                    Plane plane = Plane.CreateByNormalAndOrigin(normal, centroid);
                    Face face = ExtrusionAnalyzer.Create(solid, plane, normal).GetExtrusionBase();
                    loops = ExporterIFCUtils.ValidateCurveLoops(face.GetEdgesAsCurveLoops(), normal);
                    result = face.GetBoundingBox();
                    status = trx.Commit();
                }
                catch (Exception)
                {
                    if (!trx.HasEnded())
                    {
                        status = trx.RollBack();
                    }
                }
            }
            return result;
        }


        public static IList<CurveLoop> GetSectionSize(this Solid solid, Document doc, XYZ normal, in XYZ centroid, out double width, out double height)
        {
            width = 0; height = 0;
            BoundingBoxUV size = solid.GetSectionBound(doc, normal, in centroid, out IList<CurveLoop> loops);
            if (size != null && normal.IsAlmostEqualTo(XYZ.BasisX, 0.5))
            {
                width = Math.Round(size.Max.U - size.Min.U, 5);
                height = Math.Round(size.Max.V - size.Min.V, 5);
            }
            if (size != null && normal.IsAlmostEqualTo(XYZ.BasisY, 0.5))
            {
                width = Math.Round(size.Max.V - size.Min.V, 5);
                height = Math.Round(size.Max.U - size.Min.U, 5);
            }
            return loops;
        }


        public static Solid CreateExtrusionGeometry(this IList<CurveLoop> curveloops, in XYZ normal, in double height, in double offset)
        {
            double half = height / 2;
            List<CurveLoop> profileLoops = new(5);
            foreach (CurveLoop loop in curveloops)
            {
                CurveLoop newloop = CurveLoop.CreateViaOffset(loop, offset, normal);
                Transform trs = Transform.CreateTranslation(normal * half);
                newloop.Transform(trs.Inverse);
                profileLoops.Add(newloop);
            }
            return GeometryCreationUtilities.CreateExtrusionGeometry(profileLoops, normal, height);
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


        public static bool GetIntersectingLinkedElementIds(this Solid solid, IList<RevitLinkInstance> links, out List<ElementId> ids)
        {
            ids = new List<ElementId>();
            FilteredElementCollector intersecting;
            foreach (RevitLinkInstance lnk in links)
            {
                Document doc = lnk.GetLinkDocument();
                Transform transform = lnk.GetTransform();
                if (!transform.AlmostEqual(Transform.Identity))
                {
                    solid = SolidUtils.CreateTransformed(solid, transform.Inverse);
                }

                intersecting = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).WherePasses(new ElementIntersectsSolidFilter(solid));
                ids.AddRange(intersecting.ToElementIds());
            }
            return ids.Any();
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


        public static double GetHorizontAngle(this XYZ normal)
        {
            return Math.Atan(normal.X / normal.Y);
        }


        public static void GetAngleBetween(this XYZ normal, in XYZ vector, out double horizont, out double vertical)
        {
            double sin = (normal.X * vector.Y) - (vector.X * normal.Y);
            double cos = (normal.X * vector.X) + (normal.Y * vector.Y);
            vertical = NormaliseAngle(Math.Asin(normal.Z - vector.Z));
            horizont = NormaliseAngle(Math.Atan2(sin, cos));
        }


        private static double NormaliseAngle(double angle)
        {
            angle = Math.Abs(angle);
            angle = angle > Math.PI ? (Math.PI * 2) - angle : angle;
            angle = angle > (Math.PI / 2) ? Math.PI - angle : angle;
            return Math.Abs(angle);
        }


        static XYZ ConvertToPositive(this XYZ vector)
        {
            return new XYZ(Math.Abs(vector.X), Math.Abs(vector.Y), Math.Abs(vector.Z));
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

