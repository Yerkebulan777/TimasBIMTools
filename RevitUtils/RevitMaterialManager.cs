using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitMaterialManager
    {

        public static SortedDictionary<string, Material> GetAllConstructionStructureMaterials(Document doc)
        {
            Material material = null;
            Material categoryMaterial = null;
            FilteredElementCollector collector = null;
            MaterialFunctionAssignment structure = MaterialFunctionAssignment.Structure;
            SortedDictionary<string, Material> result = new SortedDictionary<string, Material>();
            Material wallMaterial = Category.GetCategory(doc, BuiltInCategory.OST_Walls).Material;
            Material floorMaterial = Category.GetCategory(doc, BuiltInCategory.OST_Floors).Material;
            Material roofMaterial = Category.GetCategory(doc, BuiltInCategory.OST_Roofs).Material;
            collector = collector.UnionWith(RevitFilterManager.GetInstancesOfCategory(doc, typeof(WallType), BuiltInCategory.OST_Walls, false));
            collector = collector.UnionWith(RevitFilterManager.GetInstancesOfCategory(doc, typeof(FloorType), BuiltInCategory.OST_Floors, false));
            collector = collector.UnionWith(RevitFilterManager.GetInstancesOfCategory(doc, typeof(RoofType), BuiltInCategory.OST_Roofs, false));
            foreach (Element elem in collector)
            {
                CompoundStructure comStruct = null;
                if (elem is WallType wallType)
                {
                    categoryMaterial = wallMaterial;
                    comStruct = wallType.GetCompoundStructure();
                }
                else if (elem is FloorType floorType)
                {
                    categoryMaterial = floorMaterial;
                    comStruct = floorType.GetCompoundStructure();
                }
                else if (elem is RoofType roofType)
                {
                    categoryMaterial = roofMaterial;
                    comStruct = roofType.GetCompoundStructure();
                }

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

            collector.Dispose();
            return result;

        }



        //public static IEnumerable<ElementId> GetElementIds(Document doc)
        //{

        //}
    }
}
