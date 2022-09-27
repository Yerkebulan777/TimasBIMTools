using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitMaterialManager
    {
        public static SortedDictionary<string, Material> GetAllConstructionStructureMaterials(Document doc)
        {
            List<Element> elements = new List<Element>(50);
            MaterialFunctionAssignment structure = MaterialFunctionAssignment.Structure;
            SortedDictionary<string, Material> result = new SortedDictionary<string, Material>();
            Material roofMaterial = Category.GetCategory(doc, BuiltInCategory.OST_Roofs).Material;
            Material wallMaterial = Category.GetCategory(doc, BuiltInCategory.OST_Walls).Material;
            Material floorMaterial = Category.GetCategory(doc, BuiltInCategory.OST_Floors).Material;
            elements.AddRange(RevitFilterManager.GetInstancesOfCategory(doc, typeof(RoofType), BuiltInCategory.OST_Roofs, false).ToElements());
            elements.AddRange(RevitFilterManager.GetInstancesOfCategory(doc, typeof(WallType), BuiltInCategory.OST_Walls, false).ToElements());
            elements.AddRange(RevitFilterManager.GetInstancesOfCategory(doc, typeof(FloorType), BuiltInCategory.OST_Floors, false).ToElements());
            foreach (Element elem in elements)
            {
                Material material = null;
                Material categoryMaterial = null;
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
                                }
                                finally
                                {
                                    material = material ?? categoryMaterial;
                                    result[material.Name] = material;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            return result;
        }


        //public static IEnumerable<ElementId> GetElementIds(Document doc)
        //{

        //}
    }
}
