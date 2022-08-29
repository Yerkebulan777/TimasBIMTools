using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace RevitTimasBIMTools.RevitUtils
{
    internal class RevitMaterialManager
    {
        private static Categories allCategories = null;
        private static readonly MaterialFunctionAssignment structure = MaterialFunctionAssignment.Structure;

        public static Dictionary<string, string> GetAllConstructionStructureMaterials(Document doc)
        {
            allCategories = doc.Settings.Categories;
            Dictionary<string, string> result = new Dictionary<string, string>();
            FilteredElementCollector collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(WallType), BuiltInCategory.OST_Walls);
            foreach (Element elem in collector)
            {
                Tuple<string, Material> temp = GetStructureMaterial(doc, elem);
                result[temp.Item1] = temp.Item2.Name;
            }

            collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(FloorType), BuiltInCategory.OST_Floors);
            foreach (Element elem in collector)
            {
                Tuple<string, Material> temp = GetStructureMaterial(doc, elem);
                result[temp.Item1] = temp.Item2.Name;
            }

            collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(RoofType), BuiltInCategory.OST_Roofs);
            foreach (Element elem in collector)
            {
                Tuple<string, Material> temp = GetStructureMaterial(doc, elem);
                result[temp.Item1] = temp.Item2.Name;
            }
            collector.Dispose();
            return result;
        }


        private static Tuple<string, Material> GetStructureMaterial(Document doc, Element elem)
        {
            string name = null;
            Material material = null;
            CompoundStructure comStruct = null; 
            if (elem is WallType wallType && wallType != null)
            {
                comStruct = wallType.GetCompoundStructure();
                foreach (CompoundStructureLayer structLayer in comStruct.GetLayers())
                {
                    if (structure == structLayer.Function)
                    {
                        try
                        {
                            material = doc.GetElement(structLayer.MaterialId) as Material;
                            if (null == material)
                            {
                                material = allCategories.get_Item(BuiltInCategory.OST_WallsStructure).Material;
                            }
                        }
                        finally
                        {
                            name = wallType.Name;
                        }
                        break;
                    }
                }
            }
            if (elem is FloorType floorType && floorType != null)
            {
                comStruct = floorType.GetCompoundStructure();
                foreach (CompoundStructureLayer structLayer in comStruct.GetLayers())
                {
                    if (structure == structLayer.Function)
                    {
                        try
                        {
                            material = doc.GetElement(structLayer.MaterialId) as Material;
                            if (null == material)
                            {
                                material = allCategories.get_Item(BuiltInCategory.OST_FloorsStructure).Material;
                            }
                        }
                        finally
                        {
                            name = floorType.Name;
                        }
                        break;
                    }
                }
            }
            if (elem is RoofType roofType && roofType != null)
            {
                comStruct = roofType.GetCompoundStructure();
                foreach (CompoundStructureLayer structLayer in comStruct.GetLayers())
                {
                    if (structure == structLayer.Function)
                    {
                        try
                        {
                            material = doc.GetElement(structLayer.MaterialId) as Material;
                            if (null == material)
                            {
                                material = allCategories.get_Item(BuiltInCategory.OST_RoofsStructure).Material;
                            }
                        }
                        finally
                        {
                            name = roofType.Name;
                        }
                        break;
                    }
                }
            }
            return Tuple.Create(name, material);
        }

    }
}
