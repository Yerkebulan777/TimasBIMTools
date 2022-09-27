using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using Material = Autodesk.Revit.DB.Material;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitMaterialManager
    {
        public static List<Element> StructureElementTypeList = new List<Element>(100);
        public static IDictionary<string, Material> GetAllConstructionStructureMaterials(Document doc)
        {
            CompoundStructure compound = null;
            StructureElementTypeList.Clear();
            List<Element> elements = new List<Element>(100);
            IDictionary<string, Material> result = new SortedDictionary<string, Material>();
            StructureElementTypeList.AddRange(RevitFilterManager.GetElementsOfCategory(doc, typeof(RoofType), BuiltInCategory.OST_Roofs, false));
            StructureElementTypeList.AddRange(RevitFilterManager.GetElementsOfCategory(doc, typeof(WallType), BuiltInCategory.OST_Walls, false));
            StructureElementTypeList.AddRange(RevitFilterManager.GetElementsOfCategory(doc, typeof(FloorType), BuiltInCategory.OST_Floors, false));
            foreach (Element elem in StructureElementTypeList)
            {
                if (elem is RoofType roofType)
                {
                    compound = roofType.GetCompoundStructure();
                }
                else if (elem is WallType wallType)
                {
                    compound = wallType.GetCompoundStructure();
                }
                else if (elem is FloorType floorType)
                {
                    compound = floorType.GetCompoundStructure();
                }
                Material material = GetCompoundStructureMaterial(doc, elem, compound);
                if (material != null)
                {
                    result[material.Name] = material;
                    elements.Add(elem);
                }
            }
            StructureElementTypeList = elements;
            return result;
        }


        public static IList<Element> GetTypeIdsByStructureMaterial(Document doc, string materialName)
        {
            List<Element> result = new List<Element>(100);
            foreach (Element elem in StructureElementTypeList)
            {
                CompoundStructure compound = null;
                if (elem is RoofType roofType)
                {
                    compound = roofType.GetCompoundStructure();
                }
                else if (elem is WallType wallType)
                {
                    compound = wallType.GetCompoundStructure();
                }
                else if (elem is FloorType floorType)
                {
                    compound = floorType.GetCompoundStructure();
                }
                Material material = GetCompoundStructureMaterial(doc, elem, compound);
                if (material != null && material.Name == materialName)
                {
                    _ = RevitFilterManager.GetInstancesByTypeId(doc, elem.Category.Id, elem.Id);
                }
            }
            return result;
        }


        private static Material GetCompoundStructureMaterial(Document doc, Element element, CompoundStructure compound)
        {
            double tolerance = 0;
            Material material = null;
            if (compound != null)
            {
                MaterialFunctionAssignment function = MaterialFunctionAssignment.Structure;
                foreach (CompoundStructureLayer layer in compound.GetLayers())
                {
                    if (function == layer.Function && tolerance < layer.Width)
                    {
                        material = doc.GetElement(layer.MaterialId) as Material;
                        material = material ?? element.Category.Material;
                        tolerance = Math.Round(layer.Width, 3);
                    }
                }
            }
            return material;
        }
    }
}
