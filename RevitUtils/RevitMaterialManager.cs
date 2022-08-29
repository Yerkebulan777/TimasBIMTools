using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitMaterialManager
    {
        public static Dictionary<string, string> GetAllConstructionStructureMaterials(Document doc)
        {
            Material material = null;
            Dictionary<string, string> result = new Dictionary<string, string>();
            MaterialFunctionAssignment structure = MaterialFunctionAssignment.Structure;

            Material categoryMaterial = Category.GetCategory(doc, BuiltInCategory.OST_Walls).Material;
            FilteredElementCollector collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(WallType), BuiltInCategory.OST_Walls);
            CompoundStructure comStruct;
            foreach (Element elem in collector)
            {
                if (elem is WallType wallType)
                {
                    comStruct = wallType.GetCompoundStructure();
                    if (comStruct != null)
                    {
                        foreach (CompoundStructureLayer structLayer in comStruct.GetLayers())
                        {
                            if (structure == structLayer.Function)
                            {
                                if (structure == structLayer.Function)
                                {
                                    try
                                    {
                                        material = doc.GetElement(structLayer.MaterialId) as Material;
                                        material = material ?? categoryMaterial;
                                    }
                                    finally
                                    {
                                        if (null != material)
                                        {
                                            result[wallType.Name] = material.Name;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            categoryMaterial = Category.GetCategory(doc, BuiltInCategory.OST_Floors).Material;
            collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(FloorType), BuiltInCategory.OST_Floors);
            foreach (Element elem in collector)
            {
                if (elem is FloorType floorType)
                {
                    comStruct = floorType.GetCompoundStructure();
                    if (comStruct != null)
                    {
                        foreach (CompoundStructureLayer structLayer in comStruct.GetLayers())
                        {
                            if (structure == structLayer.Function)
                            {
                                if (structure == structLayer.Function)
                                {
                                    try
                                    {
                                        material = doc.GetElement(structLayer.MaterialId) as Material;
                                        material = material ?? categoryMaterial;
                                    }
                                    finally
                                    {
                                        if (null != material)
                                        {
                                            result[floorType.Name] = material.Name;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            categoryMaterial = Category.GetCategory(doc, BuiltInCategory.OST_Roofs).Material;
            collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(RoofType), BuiltInCategory.OST_Roofs);
            foreach (Element elem in collector)
            {
                if (elem is RoofType roofType)
                {
                    comStruct = roofType.GetCompoundStructure();
                    if (comStruct != null)
                    {
                        foreach (CompoundStructureLayer structLayer in comStruct.GetLayers())
                        {
                            if (structure == structLayer.Function)
                            {
                                try
                                {
                                    material = doc.GetElement(structLayer.MaterialId) as Material;
                                    material = material ?? categoryMaterial;
                                }
                                finally
                                {
                                    if (null != material)
                                    {
                                        result[roofType.Name] = material.Name;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }

            collector.Dispose();
            return result;

        }
    }
}
