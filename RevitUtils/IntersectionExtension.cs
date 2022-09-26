using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitTimasBIMTools.Services;

namespace RevitTimasBIMTools.RevitUtils
{
    internal static class IntersectionExtension
    {
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

        public static Solid GetElementCenterSolid(this Element element, Options geoOptions, Transform global, XYZ centre, double tolerance = 0.5)
        {
            Solid result = null;
            double maxDistance = double.MaxValue;
            GeometryElement geomElem = element.get_Geometry(geoOptions);
            foreach (GeometryObject geomObj in geomElem.GetTransformed(global))
            {
                if (geomObj is Solid)
                {
                    Solid solid = geomObj as Solid;
                    if (solid.Faces.Size > 0 && solid.Volume > tolerance)
                    {
                        double minDistance = Math.Abs(centre.DistanceTo(solid.ComputeCentroid()));
                        if (maxDistance > minDistance)
                        {
                            maxDistance = minDistance;
                            result = solid;
                        }
                    }
                }
                else if (geomObj is GeometryInstance geomInst)
                {
                    GeometryElement instGeomElem = geomInst.GetInstanceGeometry();
                    foreach (GeometryObject instGeomObj in instGeomElem)
                    {
                        if (instGeomObj is Solid solid)
                        {
                            if (solid.Faces.Size > 0 && solid.Volume > tolerance)
                            {
                                double minDistance = Math.Abs(centre.DistanceTo(solid.ComputeCentroid()));
                                if (maxDistance > minDistance)
                                {
                                    maxDistance = minDistance;
                                    result = solid;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static Solid CreateSolidFromBoundingBox(this BoundingBoxXYZ boundBox, Transform transform, SolidOptions solidOptions = null)
        {
            // Check that the bounding box is valid.
            if (boundBox != null && boundBox.Enabled)
            {
                try
                {
                    // Create a transform based on the incoming transform coordinate system and the bounding box coordinate system.
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

                    CurveLoop baseLoop = new CurveLoop();

                    for (int i = 0; i < 4; i++)
                    {
                        baseLoop.Append(Line.CreateBound(profilePts[i], profilePts[(i + 1) % 4]));
                    }

                    IList<CurveLoop> baseLoops = new List<CurveLoop> { baseLoop };

                    return solidOptions == null
                        ? GeometryCreationUtilities.CreateExtrusionGeometry(baseLoops, extrusionDirection, extrusionDistance)
                        : GeometryCreationUtilities.CreateExtrusionGeometry(baseLoops, extrusionDirection, extrusionDistance, solidOptions);
                }
                catch (Exception exc)
                {
                    Logger.Error(exc.Message);
                    return null;
                }
            }
            return null;
        }

    }
}
