using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;
using Options = Autodesk.Revit.DB.Options;


namespace RevitTimasBIMTools.RevitUtils
{
    internal static class GeometryExtension
    {
        private static Solid result { get; set; } = null;

        public static Outline GetOutLine(this BoundingBoxXYZ bbox)
        {
            Transform transform = bbox.Transform;
            return new Outline(transform.OfPoint(bbox.Min), transform.OfPoint(bbox.Max));
        }


        public static XYZ GetMiddlePointByBoundingBox(this Element element, ref BoundingBoxXYZ bbox)
        {
            bbox = element.get_BoundingBox(null);
            return (bbox.Min + bbox.Max) * 0.5;
        }


        public static XYZ GetNormalByTopFace(this Element elem, Transform local, double tollerance = 0)
        {
            XYZ result = XYZ.BasisZ;
            foreach (Reference refFace in HostObjectUtils.GetTopFaces(elem as HostObject))
            {
                GeometryObject geo = elem.GetGeometryObjectFromReference(refFace);
                Face face = geo as Face;
                XYZ normal = XYZ.BasisZ;
                try
                {
                    if (face is PlanarFace planar && planar != null)
                    {
                        normal = planar.FaceNormal;
                    }
                    else
                    {
                        BoundingBoxUV box = face.GetBoundingBox();
                        normal = face.ComputeNormal((box.Max + box.Min) / 2);
                        normal = local.OfVector(normal);
                    }
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
                {
                    Logger.Error(ex.Message);
                    continue;
                }
                finally
                {
                    if (face.Area > tollerance)
                    {
                        result = normal.Normalize();
                    }
                }
            }
            return result;
        }


        public static Solid GetSolidByVolume(this Element element, Transform global, Options options, double tolerance = 0.5)
        {
            result = null;
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
            result = null;
            GeometryElement geomElement = elem.get_Geometry(options);
            BooleanOperationsType unionType = BooleanOperationsType.Union;
            foreach (GeometryObject obj in geomElement.GetTransformed(global))
            {
                if (obj is Solid solid && solid != null && solid.Faces.Size > 0)
                {
                    if (solid.Volume > tolerance)
                    {
                        if (null == result)
                        {
                            result = solid;
                        }
                        try
                        {
                            result = BooleanOperationsUtils.ExecuteBooleanOperation(result, solid, unionType);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
            return result;
        }


        public static Solid GetIntersectionSolid(this Solid source, Element elem, Transform global, Options options, double tolerance = 0)
        {
            result = null;
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


        public static IList<ModelCurveArray> GetCountours(this Solid solid, Document doc, Plane plane, SketchPlane sketch, double offset = 0)
        {
            XYZ direction = plane.Normal;
            IList<ModelCurveArray> curves = new List<ModelCurveArray>();
            using (Transaction t = new(doc))
            {
                _ = t.Start("GetCountours");
                try
                {
                    Face face = ExtrusionAnalyzer.Create(solid, plane, direction).GetExtrusionBase();
                    IList<CurveLoop> curveloops = ExporterIFCUtils.ValidateCurveLoops(face.GetEdgesAsCurveLoops(), direction);
                    foreach (CurveLoop loop in curveloops)
                    {
                        CurveArray array = ConvertLoopToArray(CurveLoop.CreateViaOffset(loop, offset, direction));
                        if (!array.IsEmpty)
                        {
                            curves.Add(doc.Create.NewModelCurveArray(array, sketch));
                        }
                    }
                    _ = t.Commit();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                    if (!t.HasEnded())
                    {
                        _ = t.RollBack();
                    }
                }
            }
            return curves;
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


        public static Solid CreateSolidFromBoundingBox(this BoundingBoxXYZ boundBox, Transform transform, SolidOptions options)
        {
            result = null;
            if (boundBox != null && boundBox.Enabled)
            {
                try
                {
                    // Create a global based on the incoming global coordinate system and the bounding box coordinate system.
                    Transform bboxTransform = (transform == null) ? boundBox.Transform : transform.Multiply(boundBox.Transform);

                    XYZ[] profilePts = new XYZ[4];
                    profilePts[0] = bboxTransform.OfPoint(boundBox.Min);
                    profilePts[1] = bboxTransform.OfPoint(new XYZ(boundBox.Max.X, boundBox.Min.Y, boundBox.Min.Z));
                    profilePts[2] = bboxTransform.OfPoint(new XYZ(boundBox.Max.X, boundBox.Max.Y, boundBox.Min.Z));
                    profilePts[3] = bboxTransform.OfPoint(new XYZ(boundBox.Min.X, boundBox.Max.Y, boundBox.Min.Z));

                    XYZ upperRightXYZ = bboxTransform.OfPoint(boundBox.Max);

                    // If we assumed that the transforms had no scaling, then we could simply take boundBox.Max.Z - boundBox.Min.Z.
                    // This code removes that assumption.

                    XYZ origExtrusionVector = new XYZ(boundBox.Min.X, boundBox.Min.Y, boundBox.Max.Z) - boundBox.Min;

                    XYZ extrusionVector = bboxTransform.OfVector(origExtrusionVector);

                    double extrusionDistance = extrusionVector.GetLength();
                    XYZ extrusionDirection = extrusionVector.Normalize();

                    CurveLoop baseLoop = new();

                    for (int i = 0; i < 4; i++)
                    {
                        baseLoop.Append(Line.CreateBound(profilePts[i], profilePts[(i + 1) % 4]));
                    }

                    IList<CurveLoop> baseLoops = new List<CurveLoop> { baseLoop };

                    result = GeometryCreationUtilities.CreateExtrusionGeometry(baseLoops, extrusionDirection, extrusionDistance, options);
                }
                catch (Exception exc)
                {
                    Logger.Error(exc.Message);
                }
            }
            return result;
        }


        public static Solid Sphere(this XYZ center, double radius = 0.75)
        {
            // Use the standard global coordinate system 
            // as a frame, translated to the sphere bottom.
            Frame frame = new Frame(center, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ);

            // Create a vertical half-circle loop;
            // this must be in the frame location.
            XYZ start = center - radius * XYZ.BasisZ;
            XYZ end = center + radius * XYZ.BasisZ;
            XYZ XyzOnArc = center + radius * XYZ.BasisX;

            Arc arc = Arc.Create(start, end, XyzOnArc);

            Line line = Line.CreateBound(arc.GetEndPoint(1), arc.GetEndPoint(0));

            CurveLoop halfCircle = new CurveLoop();
            halfCircle.Append(arc);
            halfCircle.Append(line);

            List<CurveLoop> loops = new List<CurveLoop>(1);
            loops.Add(halfCircle);

            return GeometryCreationUtilities.CreateRevolvedGeometry(frame, loops, 0, 2 * Math.PI);
        }


        public static void CreateDirectShape(this Solid solid, Document doc, Instance elem, BuiltInCategory builtIn = BuiltInCategory.OST_GenericModel)
        {
            using Transaction t = new(doc, "Create DirectShape");
            try
            {
                _ = t.Start();
                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(builtIn));
                ds.ApplicationDataId = elem.UniqueId;
                ds.Name = "Centroid by " + elem.Name;
                ds.SetShape(new GeometryObject[] { solid });
                _ = t.Commit();
            }
            catch (Exception exc)
            {
                Logger.Error(exc.Message);
                if (!t.HasEnded())
                {
                    _ = t.RollBack();
                }
            }
        }


        public static void GetSizeByGeometry(this Solid solid, XYZ direction)
        {
            XYZ centroid = solid.ComputeCentroid();
            direction = ResetDirectionToPositive(direction);
            Transform identityTransform = Transform.Identity;
            double angleHorisontDegrees = ConvertRadiansToDegrees(GetHorizontAngleRadiansByNormal(direction));
            double angleVerticalDegrees = ConvertRadiansToDegrees(GetVerticalAngleRadiansByNormal(direction));
            Transform horizont = Transform.CreateRotationAtPoint(identityTransform.BasisZ, GetInternalAngleByDegrees(angleHorisontDegrees), centroid);
            Transform vertical = Transform.CreateRotationAtPoint(identityTransform.BasisX, GetInternalAngleByDegrees(angleVerticalDegrees), centroid);
            solid = angleHorisontDegrees == 0 ? solid : SolidUtils.CreateTransformed(solid, horizont);
            solid = angleVerticalDegrees == 0 ? solid : SolidUtils.CreateTransformed(solid, vertical);
            BoundingBoxXYZ interBbox = solid?.GetBoundingBox();
            if (interBbox != null)
            {
                _ = Math.Abs(interBbox.Max.X - interBbox.Min.X);
                _ = Math.Abs(interBbox.Max.Z - interBbox.Min.Z);
            }
        }


        public static XYZ ResetDirectionToPositive(this XYZ direction)
        {
            double radians = XYZ.BasisX.AngleOnPlaneTo(direction, XYZ.BasisZ);
            return radians < Math.PI ? direction : direction.Negate();
        }


        public static double GetHorizontAngleRadiansByNormal(this XYZ direction)
        {
            return Math.Atan(direction.X / direction.Y);
        }


        public static double GetVerticalAngleRadiansByNormal(this XYZ direction)
        {
            return Math.Acos(direction.DotProduct(XYZ.BasisZ)) - (Math.PI / 2);
        }


        public static double GetInternalAngleByDegrees(this double degrees)
        {
            return UnitUtils.ConvertToInternalUnits(degrees, DisplayUnitType.DUT_DECIMAL_DEGREES);
        }


        public static double GetInternalAngleByRadians(this double degrees)
        {
            return UnitUtils.ConvertToInternalUnits(degrees, DisplayUnitType.DUT_RADIANS);
        }


        private static double ConvertRadiansToDegrees(double radians, int digit = 5)
        {
            return Math.Round(180 / Math.PI * radians, digit);
        }


    }
}
