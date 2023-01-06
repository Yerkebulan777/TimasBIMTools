using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using SmartBIMTools.RevitModel;
using SmartBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;
using Options = Autodesk.Revit.DB.Options;
using Plane = Autodesk.Revit.DB.Plane;


namespace SmartBIMTools.RevitUtils;

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


    public static XYZ GetHostNormal(this Element elem, double tollerance = 0)
    {
        XYZ resultNormal = XYZ.BasisZ;
        Transform local = Transform.Identity;
        if (elem is Wall wall)
        {
            resultNormal = local.OfVector(wall.Orientation).Normalize();
        }
        else if (elem is HostObject hostObject)
        {
            foreach (Reference refFace in HostObjectUtils.GetTopFaces(hostObject))
            {
                GeometryObject geo = elem.GetGeometryObjectFromReference(refFace);
                if (geo is Face face && face.Area > tollerance)
                {
                    try
                    {
                        if (face is PlanarFace planar)
                        {
                            resultNormal = local.OfVector(planar.FaceNormal).Normalize();
                        }
                        else
                        {
                            BoundingBoxUV box = face.GetBoundingBox();
                            resultNormal = face.ComputeNormal((box.Max + box.Min) * 0.5);
                            resultNormal = local.OfVector(resultNormal).Normalize();
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
                    {
                        SBTLogger.Error(ex.Message);
                    }
                }
            }
        }
        return resultNormal;
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


    public static ISet<XYZ> GetIntersectionPoints(this Solid source, in Element elem, in Transform global, in Options options, ref XYZ centroid)
    {
        ISet<XYZ> vertices = new HashSet<XYZ>(50);
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
                    if (solid != null && solid.Volume > 0)
                    {
                        centroid = solid.ComputeCentroid();
                        foreach (Face f in solid.Faces)
                        {
                            Mesh mesh = f.Triangulate();
                            int n = mesh.NumTriangles;
                            for (int i = 0; i < n; ++i)
                            {
                                MeshTriangle triangle = mesh.get_Triangle(i);
                                _ = vertices.Add(triangle.get_Vertex(0));
                                _ = vertices.Add(triangle.get_Vertex(1));
                                _ = vertices.Add(triangle.get_Vertex(2));
                            }
                        }
                    }
                }
            }
        }
        return vertices;
    }


    public static BoundingBoxUV ProjectPointsOnPlane(this Plane plane, in ISet<XYZ> points)
    {
        BoundingBoxUV result = null;
        if (plane != null && plane.IsValidObject)
        {
            double minU = 0, maxU = 0;
            double minV = 0, maxV = 0;

            foreach (XYZ pnt in points)
            {
                plane.Project(pnt, out UV uvp, out double dist);
                if (uvp != null && dist > 0)
                {
                    minU = Math.Min(minU, uvp.U);
                    maxU = Math.Max(maxU, uvp.U);
                    minV = Math.Min(minV, uvp.V);
                    maxV = Math.Max(maxV, uvp.V);
                }
            }

            result = new BoundingBoxUV(minU, minV, maxU, maxV);
        }
        return result;
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


    public static IList<CurveLoop> GetSectionProfileWithOffset(this ElementModel model, in double offset)
    {
        Plane plane = model.SectionPlane;
        BoundingBoxUV bbox = model.SectionBox;
        XYZ origin = model.SectionPlane.Origin;
        XYZ normal = model.SectionPlane.Normal;

        XYZ pt0 = origin + (bbox.Min.U * plane.XVec) + (bbox.Min.V * plane.YVec);
        XYZ pt1 = origin + (bbox.Max.U * plane.XVec) + (bbox.Min.V * plane.YVec);
        XYZ pt2 = origin + (bbox.Max.U * plane.XVec) + (bbox.Max.V * plane.YVec);
        XYZ pt3 = origin + (bbox.Min.U * plane.XVec) + (bbox.Max.V * plane.YVec);

        IList<Curve> edges = new List<Curve>(4)
        {
            Line.CreateBound(pt0, pt1),
            Line.CreateBound(pt1, pt2),
            Line.CreateBound(pt2, pt3),
            Line.CreateBound(pt3, pt0)
        };

        CurveLoop loop = CurveLoop.Create(edges);
        if (!loop.IsCounterclockwise(normal)) { loop.Flip(); }
        loop = CurveLoop.CreateViaOffset(loop, offset, normal);
        IList<CurveLoop> curveloops = new List<CurveLoop>() { loop };

        return ExporterIFCUtils.ValidateCurveLoops(curveloops, normal);
    }


    public static Solid CreateExtrusionGeometry(this IList<CurveLoop> curveloops, in XYZ normal, in double height)
    {
        double distance = Math.Abs(Math.Round(height + 0.25, 5));
        IList<CurveLoop> profile = new List<CurveLoop>(4);
        foreach (CurveLoop loop in curveloops)
        {
            CurveLoop newloop = CurveLoop.CreateViaCopy(loop);
            Transform trs = Transform.CreateTranslation(normal * distance / 2);
            newloop.Transform(trs.Inverse);
            profile.Add(newloop);
        }
        profile = ExporterIFCUtils.ValidateCurveLoops(profile, normal);
        return GeometryCreationUtilities.CreateExtrusionGeometry(profile, normal, distance);
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
            SBTLogger.Error(exc.Message);
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


    public static XYZ ToPositive(this XYZ vector)
    {
        if (vector.IsAlmostEqualTo(XYZ.BasisZ, 0.5))
        {
            return vector;
        }
        else if (vector.IsAlmostEqualTo(XYZ.BasisX, 0.5))
        {
            return vector;
        }
        else if (vector.IsAlmostEqualTo(XYZ.BasisY, 0.5))
        {
            return vector;
        }
        return vector.Negate();
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

