using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;
using Level = Autodesk.Revit.DB.Level;

namespace RevitTimasBIMTools.RevitUtils
{
    internal sealed class RevitFilterManager
    {

        public static List<Element> ElementTypeList = new(100);


        #region Standert Filtered Element Collector

        public static FilteredElementCollector GetElementsOfCategory(Document doc, Type type, BuiltInCategory bic, bool? isInstances = null)
        {
            return isInstances.HasValue
                ? (bool)isInstances == true
                    ? new FilteredElementCollector(doc).OfClass(type).OfCategory(bic).WhereElementIsNotElementType()
                    : new FilteredElementCollector(doc).OfClass(type).OfCategory(bic).WhereElementIsElementType()
                : new FilteredElementCollector(doc).OfClass(type).OfCategory(bic);
        }


        public static FilteredElementCollector ParamFilterRuleSymbolName(Document doc, BuiltInCategory BultCat, string familyTypeName)
        {
            return new FilteredElementCollector(doc).OfCategory(BultCat).OfClass(typeof(FamilyInstance))
            .WherePasses(
                        new ElementParameterFilter(
                        new FilterStringRule(
                        new ParameterValueProvider(
                        new ElementId(BuiltInParameter.ELEM_FAMILY_PARAM)),
                        new FilterStringEquals(), familyTypeName, true)));
        }

        #endregion


        #region Advance Filtered Element Collector

        public static IEnumerable<Element> GetInstancesByTypeId(Document doc, ElementId catId, ElementId typeId)
        {
            int typeIntId = typeId.IntegerValue;
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfCategoryId(catId);
            foreach (Element elem in collector.WhereElementIsNotElementType())
            {
                ElementId elemTypeId = elem.GetTypeId();
                if (typeIntId.Equals(elemTypeId.IntegerValue))
                {
                    yield return elem;
                }
            }
        }


        public static FamilySymbol FindFamilySymbol(Document doc, string familyName, string symbolName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Family));

            foreach (Family f in collector)
            {
                if (f.Name.Equals(familyName))
                {
                    ISet<ElementId> ids = f.GetFamilySymbolIds();
                    foreach (ElementId id in ids)
                    {
                        FamilySymbol symbol = doc.GetElement(id) as FamilySymbol;
                        if (symbol.Name == symbolName)
                        {
                            return symbol;
                        }
                    }
                }
            }

            return null;
        }


        public static Element GetFirstElementOfTypeNamed(Document doc, Type type, string name)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(type);
            bool nameEquals(Element e)
            {
                return e.Name.Equals(name);
            }

            return collector.Any(nameEquals) ? collector.First(nameEquals) : null;
        }


        public static ElementType GetElementTypeByName(Document doc, string name)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(ElementType))
                    .First(q => q.Name.Equals(name)) as ElementType;
        }


        public static ElementType GetFamilySymbolByName(Document doc, string name)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                    .First(q => q.Name.Equals(name)) as FamilySymbol;
        }


        public static List<View3D> Get3DViews(Document document, bool template = false, string name = null)
        {
            List<View3D> elements = new();
            int invalidId = ElementId.InvalidElementId.IntegerValue;
            FilteredElementCollector collector = new FilteredElementCollector(document).OfClass(typeof(View3D));
            foreach (View3D view in collector)
            {
                if (invalidId == view.Id.IntegerValue)
                {
                    continue;
                }
                if (template == view.IsTemplate)
                {
                    continue;
                }
                if (null != name)
                {
                    if (view.Name.Contains(name))
                    {
                        continue;
                    }
                }
                elements.Add(view);
            }
            collector.Dispose();
            return elements;
        }


        public static Element GetFirst3dView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
            return collector.Cast<View3D>().First(v3 => !v3.IsTemplate);
        }
        #endregion


        #region Parameter Filter Factory

        /// <summary>Contains functions that create appropriate FilterRule objects based on the parameters given </summary>
        /// <param name="ruleSwitch"> 
        /// 0 = CreateEqualsRule;
        /// 1 = CreateGreaterOrEqualRule;
        /// 2 = CreateGreaterOrEqualRule;
        /// 3 = CreateNotEqualsRule;
        /// </param>
        /// <returns> FilteredElementCollector </returns>
        public static FilteredElementCollector ParamFilterFactory(FilteredElementCollector collector, ElementId paramId, int value, int ruleSwitch = 0)
        {
            FilterRule filterRule;
            switch (ruleSwitch)
            {
                case 0:
                    filterRule = ParameterFilterRuleFactory.CreateEqualsRule(paramId, value);
                    break;
                case 1:
                    filterRule = ParameterFilterRuleFactory.CreateGreaterOrEqualRule(paramId, value);
                    break;
                case 2:
                    filterRule = ParameterFilterRuleFactory.CreateGreaterOrEqualRule(paramId, value);
                    break;
                case 3:
                    filterRule = ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, value);
                    break;
                default:
                    return collector;
            }
            return collector.WherePasses(new ElementParameterFilter(filterRule));
        }


        public static FilteredElementCollector ParamFilterFactory(FilteredElementCollector collector, ElementId paramId, string value, int ruleSwitch = 0)
        {
            FilterRule filterRule;
            switch (ruleSwitch)
            {
                case 0:
                    filterRule = ParameterFilterRuleFactory.CreateContainsRule(paramId, value, false);
                    break;
                case 1:
                    filterRule = ParameterFilterRuleFactory.CreateBeginsWithRule(paramId, value, false);
                    break;
                case 2:
                    filterRule = ParameterFilterRuleFactory.CreateEndsWithRule(paramId, value, false);
                    break;
                case 3:
                    filterRule = ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, value, false);
                    break;
                default:
                    return collector;
            }
            return collector.WherePasses(new ElementParameterFilter(filterRule));
        }


        public static FilteredElementCollector ParamFilterFactory(FilteredElementCollector collector, ElementId paramId, double value, int ruleSwitch = 0)
        {
            const double epsilon = 1.0e-3;
            FilterRule filterRule;
            switch (ruleSwitch)
            {
                case 0:
                    filterRule = ParameterFilterRuleFactory.CreateEqualsRule(paramId, value, epsilon);
                    break;
                case 1:
                    filterRule = ParameterFilterRuleFactory.CreateGreaterOrEqualRule(paramId, value, epsilon);
                    break;
                case 2:
                    filterRule = ParameterFilterRuleFactory.CreateGreaterOrEqualRule(paramId, value, epsilon);
                    break;
                case 3:
                    filterRule = ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, value, epsilon);
                    break;
                default:
                    return collector;
            }
            return collector.WherePasses(new ElementParameterFilter(filterRule));
        }

        #endregion


        #region Material Filter

        public static IDictionary<string, Material> GetConstructionCoreMaterials(Document doc, IDictionary<int, ElementId> typeIds)
        {
            CompoundStructure compound = null;
            SortedDictionary<string, Material> result = new();
            foreach (KeyValuePair<int, ElementId> item in typeIds)
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

                if (GetInstancesByTypeId(doc, elem.Category.Id, elem.Id).Any())
                {
                    Material material = GetCompoundStructureMaterial(doc, elem, compound);
                    if (material != null)
                    {
                        result[material.Name] = material;
                    }
                }

            }
            return result;
        }


        public static IList<Element> GetInstancesByCoreMaterial(Document doc, IDictionary<int, ElementId> typeIds, string matName)
        {
            List<Element> result = new(100);
            CompoundStructure compound = null;
            foreach (KeyValuePair<int, ElementId> item in typeIds)
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
                Material material = GetCompoundStructureMaterial(doc, elem, compound);
                if (material != null && material.Name == matName)
                {
                    result.AddRange(GetInstancesByTypeId(doc, elem.Category.Id, elem.Id));
                }
            }

            return result;
        }


        private static Material GetCompoundStructureMaterial(Document doc, Element element, CompoundStructure compound)
        {
            Material material = null;
            if (compound != null)
            {
                double tolerance = 0.05;
                MaterialFunctionAssignment function = MaterialFunctionAssignment.Structure;
                foreach (CompoundStructureLayer layer in compound.GetLayers())
                {
                    if (function == layer.Function && tolerance < layer.Width)
                    {
                        material = doc.GetElement(layer.MaterialId) as Material;
                        material ??= element.Category.Material;
                        tolerance = Math.Round(layer.Width, 3);
                    }
                }
            }
            return material;
        }

        #endregion


        #region Category filter

        public static IEnumerable<Category> GetCategories(Document doc, bool model = true)
        {
            foreach (ElementId catId in ParameterFilterUtilities.GetAllFilterableCategories())
            {
                Category cat = Category.GetCategory(doc, catId);
                if (cat is not null && cat.AllowsBoundParameters)
                {
                    if (cat.CategoryType == CategoryType.Model && model)
                    {
                        yield return cat;
                    }
                }
            }
        }


        public static IDictionary<string, Category> GetEngineerCategories(Document doc)
        {
            IDictionary<string, Category> result = new SortedDictionary<string, Category>();
            IList<BuiltInCategory> builtInCats = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Conduit,
                BuiltInCategory.OST_CableTray,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_DuctCurves,
                BuiltInCategory.OST_GenericModel,
                BuiltInCategory.OST_MechanicalEquipment
            };
            foreach (BuiltInCategory catId in builtInCats)
            {
                Category cat = Category.GetCategory(doc, catId);
                if (cat != null)
                {
                    result[cat.Name] = cat;
                }
            }
            return result;
        }


        #endregion





        #region LevelFilter

        public static IDictionary<double, Level> GetValidLevels(Document doc)
        {
            IDictionary<double, Level> result = new SortedDictionary<double, Level>();
            ICollection<ElementId> wallIds = GetElementsOfCategory(doc, typeof(Wall), BuiltInCategory.OST_Walls).ToElementIds();
            ICollection<ElementId> floorIds = GetElementsOfCategory(doc, typeof(Floor), BuiltInCategory.OST_Floors).ToElementIds();
            FilteredElementCollector collector = new(doc);
            bool isValid = wallIds.Any() && floorIds.Any();
            foreach (Level level in collector.OfClass(typeof(Level)))
            {
                if (isValid)
                {
                    ElementLevelFilter levelFilter = new(level.Id);
                    collector = new FilteredElementCollector(doc, wallIds);
                    if (collector.WherePasses(levelFilter).Any())
                    {
                        collector = new FilteredElementCollector(doc, floorIds);
                        if (collector.WherePasses(levelFilter).Any())
                        {
                            result[level.ProjectElevation] = level;
                        }
                    }
                }
                else
                {
                    result[level.ProjectElevation] = level;
                }
            }
            return result;
        }

        #endregion


        #region FamilyFilter

        public static IDictionary<string, FamilySymbol> GetHostedFamilySymbols(Document doc, BuiltInCategory bic)
        {
            FamilyPlacementType placement = FamilyPlacementType.OneLevelBasedHosted;
            IDictionary<string, FamilySymbol> result = new SortedDictionary<string, FamilySymbol>();
            FilteredElementCollector collector = GetElementsOfCategory(doc, typeof(FamilySymbol), bic);
            foreach (FamilySymbol smb in collector)
            {
                Family fam = smb.Family;
                if (fam.IsValidObject && fam.IsEditable)
                {
                    if (fam.FamilyPlacementType.Equals(placement))
                    {
                        result[smb.Name] = smb;
                    }
                }
            }
            return result;
        }

        #endregion

    }
}
