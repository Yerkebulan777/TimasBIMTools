using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitMaterialManager
    {
        public static IDictionary<string, Material> GetAllConstructionStructureMaterials(Document doc)
        {
            List<Element> elements = new List<Element>(100);
            IDictionary<string, Material> result = new SortedDictionary<string, Material>();
            Material roofMat = Category.GetCategory(doc, BuiltInCategory.OST_Roofs).Material;
            Material wallMat = Category.GetCategory(doc, BuiltInCategory.OST_Walls).Material;
            Material floorMat = Category.GetCategory(doc, BuiltInCategory.OST_Floors).Material;
            MaterialFunctionAssignment structureFunction = MaterialFunctionAssignment.Structure;
            elements.AddRange(RevitFilterManager.GetInstancesOfCategory(doc, typeof(RoofType), BuiltInCategory.OST_Roofs, false).ToElements());
            elements.AddRange(RevitFilterManager.GetInstancesOfCategory(doc, typeof(WallType), BuiltInCategory.OST_Walls, false).ToElements());
            elements.AddRange(RevitFilterManager.GetInstancesOfCategory(doc, typeof(FloorType), BuiltInCategory.OST_Floors, false).ToElements());
            foreach (Element elem in elements)
            {
                Material material = null;
                Material catMaterial = null;
                CompoundStructure compound = null;
                if (elem is WallType wallType)
                {
                    catMaterial = wallMat;
                    compound = wallType.GetCompoundStructure();
                }
                else if (elem is FloorType floorType)
                {
                    catMaterial = floorMat;
                    compound = floorType.GetCompoundStructure();
                }
                else if (elem is RoofType roofType)
                {
                    catMaterial = roofMat;
                    compound = roofType.GetCompoundStructure();
                }

                if (compound != null)
                {
                    foreach (CompoundStructureLayer layer in compound.GetLayers())
                    {
                        if (structureFunction == layer.Function)
                        {
                            try
                            {
                                material = doc.GetElement(layer.MaterialId) as Material;
                            }
                            finally
                            {
                                material = material ?? catMaterial;
                                result[material.Name] = material;
                            }
                            break;
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
