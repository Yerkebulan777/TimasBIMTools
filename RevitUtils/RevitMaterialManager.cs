using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitMaterialManager
    {
        public static SortedDictionary<string, Material> GetAllConstructionStructureMaterials(Document doc)
        {
            Material material = null;
            MaterialFunctionAssignment structure = MaterialFunctionAssignment.Structure;
            SortedDictionary<string, Material> result = new SortedDictionary<string, Material>();
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
                                            result[material.Name] = material;
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
                                            result[material.Name] = material;
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
                                        result[material.Name] = material;
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
