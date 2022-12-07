using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RevitTimasBIMTools.RevitUtils
{
    public static class SmartBIMToolExtention
    {
        public static IDictionary<T, U> Merge<T, U>(this IDictionary<T, U> output, IDictionary<T, U> source, int capacity = 10)
        {
            output ??= new SortedList<T, U>(capacity);
            if (source != null && source.Count < 0)
            {
                foreach (KeyValuePair<T, U> item in source)
                {
                    if (output.ContainsKey(item.Key))
                    {
                        output[item.Key] = item.Value;
                    }
                    else
                    {
                        output.Add(item.Key, item.Value);
                    }
                }
            }
            return output;
        }


        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        {
            return source != null ? new ObservableCollection<T>(source) : new ObservableCollection<T>();
        }


        public static IDictionary<string, Material> GetStructureCoreMaterialData(this IDictionary<int, ElementId> sourceTypeIds, Document doc)
        {
            CompoundStructure compound = null;
            IDictionary<string, Material> result = new SortedDictionary<string, Material>();
            if (sourceTypeIds != null && sourceTypeIds.Count > 0)
            {
                foreach (KeyValuePair<int, ElementId> item in sourceTypeIds)
                {
                    Element elem = doc.GetElement(item.Value);
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
                    Material material = RevitFilterManager.GetCompoundStructureMaterial(doc, elem, compound);
                    if (material != null)
                    {
                        result[material.Name] = material;
                    }
                }
            }
            return result;
        }


        public static IEnumerable<Element> GetInstancesByTypeIdDataAndMaterial(this IDictionary<int, ElementId> sourceTypeIds, Document doc, Material structure)
        {
            string materialName = structure.Name;
            foreach (KeyValuePair<int, ElementId> item in sourceTypeIds)
            {
                Material material = null;
                Element elem = doc.GetElement(item.Value);
                if (elem is RoofType roofType)
                {
                    CompoundStructure compound = roofType.GetCompoundStructure();
                    material = RevitFilterManager.GetCompoundStructureMaterial(doc, elem, compound);
                }
                else if (elem is WallType wallType)
                {
                    CompoundStructure compound = wallType.GetCompoundStructure();
                    material = RevitFilterManager.GetCompoundStructureMaterial(doc, elem, compound);
                }
                else if (elem is FloorType floorType)
                {
                    CompoundStructure compound = floorType.GetCompoundStructure();
                    material = RevitFilterManager.GetCompoundStructureMaterial(doc, elem, compound);
                }
                if (material != null && material.Name == materialName)
                {
                    foreach (Element inst in RevitFilterManager.GetInstancesByElementTypeId(doc, elem.Id))
                    {
                        yield return inst;
                    }
                }
            }
        }


    }
}
