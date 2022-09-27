using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Material = Autodesk.Revit.DB.Material;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitMaterialManager
    {
        public static StringCollection StructureElementUniqueIds = new StringCollection();
        public static IDictionary<string, Material> GetAllConstructionStructureMaterials(Document doc)
        {
            Material categoryMat = null;
            CompoundStructure compound = null;
            StructureElementUniqueIds.Clear();
            List<Element> elements = new List<Element>(100);
            IDictionary<string, Material> result = new SortedDictionary<string, Material>();
            Material roofMat = Category.GetCategory(doc, BuiltInCategory.OST_Roofs).Material;
            Material wallMat = Category.GetCategory(doc, BuiltInCategory.OST_Walls).Material;
            Material floorMat = Category.GetCategory(doc, BuiltInCategory.OST_Floors).Material;
            elements.AddRange(RevitFilterManager.GetInstancesOfCategory(doc, typeof(RoofType), BuiltInCategory.OST_Roofs, false).ToElements());
            elements.AddRange(RevitFilterManager.GetInstancesOfCategory(doc, typeof(WallType), BuiltInCategory.OST_Walls, false).ToElements());
            elements.AddRange(RevitFilterManager.GetInstancesOfCategory(doc, typeof(FloorType), BuiltInCategory.OST_Floors, false).ToElements());
            foreach (Element elem in elements)
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
                if (material != null && 100 < StructureElementUniqueIds.Add(elem.UniqueId))
                {
                    result[material.Name] = material;
                }
            }
            elements.Clear();
            return result;
        }


        public static IEnumerable<Element> GetElementsByStructureMaterial(Document doc, string materialName)
        {
            Material categoryMat = null;
            lock (StructureElementUniqueIds.SyncRoot)
            {
                Material roofMat = Category.GetCategory(doc, BuiltInCategory.OST_Roofs).Material;
                Material wallMat = Category.GetCategory(doc, BuiltInCategory.OST_Walls).Material;
                Material floorMat = Category.GetCategory(doc, BuiltInCategory.OST_Floors).Material;
                foreach (string uid in StructureElementUniqueIds)
                {
                    CompoundStructure compound = null;
                    Element elem = doc.GetElement(uid);
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
                        yield return elem;
                    }
                }
            }
        }


        private static Material GetCompoundStructureMaterial(Document doc, CompoundStructure compound, Material categoryMat, double tolerance = 0.005)
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
