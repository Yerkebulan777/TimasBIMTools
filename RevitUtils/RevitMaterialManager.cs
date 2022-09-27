using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Material = Autodesk.Revit.DB.Material;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitMaterialManager
    {
        public static List<Element> StructureElementTypeList = new List<Element>(100);
        public static IDictionary<string, Material> GetAllConstructionStructureMaterials(Document doc)
        {
            Material categoryMat = null;
            CompoundStructure compound = null;
            StructureElementTypeList.Clear();
            List<Element> elements = new List<Element>(100);
            IDictionary<string, Material> result = new SortedDictionary<string, Material>();
            Material roofMat = Category.GetCategory(doc, BuiltInCategory.OST_Roofs).Material;
            Material wallMat = Category.GetCategory(doc, BuiltInCategory.OST_Walls).Material;
            Material floorMat = Category.GetCategory(doc, BuiltInCategory.OST_Floors).Material;
            StructureElementTypeList.AddRange(RevitFilterManager.GetElementsOfCategory(doc, typeof(RoofType), BuiltInCategory.OST_Roofs, false));
            StructureElementTypeList.AddRange(RevitFilterManager.GetElementsOfCategory(doc, typeof(WallType), BuiltInCategory.OST_Walls, false));
            StructureElementTypeList.AddRange(RevitFilterManager.GetElementsOfCategory(doc, typeof(FloorType), BuiltInCategory.OST_Floors, false));
            foreach (Element elem in StructureElementTypeList)
            {
                if (elem is RoofType roofType)
                {
                    categoryMat = roofMat;
                    compound = roofType.GetCompoundStructure();
                }
                else if (elem is WallType wallType)
                {
                    categoryMat = wallMat;
                    compound = wallType.GetCompoundStructure();
                }
                else if (elem is FloorType floorType)
                {
                    categoryMat = floorMat;
                    compound = floorType.GetCompoundStructure();
                }
                Material material = GetCompoundStructureMaterial(doc, compound, categoryMat);
                if (material != null)
                {
                    result[material.Name] = material;
                    elements.Add(elem);
                }
            }
            StructureElementTypeList = elements;
            return result;
        }


        public static IDictionary<ElementId, ElementId> GetTypeIdsByStructureMaterial(Document doc, string materialName)
        {
            Material categoryMat = null;
            IDictionary<ElementId, ElementId> result = new Dictionary<ElementId, ElementId>();
            Material roofMat = Category.GetCategory(doc, BuiltInCategory.OST_Roofs).Material;
            Material wallMat = Category.GetCategory(doc, BuiltInCategory.OST_Walls).Material;
            Material floorMat = Category.GetCategory(doc, BuiltInCategory.OST_Floors).Material;

            foreach (Element elem in StructureElementTypeList)
            {
                CompoundStructure compound = null;
                if (elem is RoofType roofType)
                {
                    categoryMat = roofMat;
                    compound = roofType.GetCompoundStructure();
                }
                else if (elem is WallType wallType)
                {
                    categoryMat = wallMat;
                    compound = wallType.GetCompoundStructure();
                }
                else if (elem is FloorType floorType)
                {
                    categoryMat = floorMat;
                    compound = floorType.GetCompoundStructure();
                }
                Material material = GetCompoundStructureMaterial(doc, compound, categoryMat);
                if (material != null && material.Name == materialName)
                {
                    result[elem.Category.Id] = elem.Id;
                }
            }

            return result;
        }


        private static Material GetCompoundStructureMaterial(Document doc, CompoundStructure compound, Material categoryMat, double tolerance = 0)
        {
            Material material = null;
            if (compound != null)
            {
                MaterialFunctionAssignment function = MaterialFunctionAssignment.Structure;
                foreach (CompoundStructureLayer layer in compound.GetLayers())
                {
                    if (function == layer.Function && tolerance < layer.Width)
                    {
                        material = doc.GetElement(layer.MaterialId) as Material;
                        tolerance = Math.Round(layer.Width, 3);
                        material = material ?? categoryMat;
                    }
                }
            }
            return material;
        }
    }
}
