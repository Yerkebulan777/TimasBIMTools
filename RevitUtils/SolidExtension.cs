using Autodesk.Revit.DB;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using Options = Autodesk.Revit.DB.Options;

namespace RevitTimasBIMTools.RevitUtils
{
    internal static class SolidExtension
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


        public static Solid GetIntersectionSolid(this Element elem, Transform global, Solid source, Options options, double tolerance = 0)
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
    }
}
