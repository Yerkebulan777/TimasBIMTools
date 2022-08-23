﻿using Autodesk.Revit.DB;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTimasBIMTools.RevitUtils
{
    sealed class RevitFilterManager
    {

        #region Standert Filtered Element Collector

        public static FilteredElementCollector GetInstancesOfCategory(Document doc, Type type, BuiltInCategory bic)
        {
            return new FilteredElementCollector(doc).OfClass(type).OfCategory(bic);
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

        #endregion // Filtered Element Collector


        #region Advance Filtered Element Collector
        public static FamilySymbol FindFamilySymbol(Document doc, string familyName, string symbolName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Family));

            foreach (Family f in collector)
                if (f.Name.Equals(familyName))
                {
                    ISet<ElementId> ids = f.GetFamilySymbolIds();
                    foreach (ElementId id in ids)
                    {
                        FamilySymbol symbol = doc.GetElement(id) as FamilySymbol;
                        if (symbol.Name == symbolName) return symbol;
                    }
                }
            return null;
        }


        public static Element GetFirstElementOfTypeNamed(Document doc, Type type, string name)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(type);
            Func<Element, bool> nameEquals = e => e.Name.Equals(name);
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
            List<View3D> elements = new List<View3D>();
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


        #region Category filter

        public IList<BuiltInCategory> GetFitrableCategories(Document document)
        {
            List<BuiltInCategory> output = new List<BuiltInCategory>();
            foreach (ElementId catId in ParameterFilterUtilities.GetAllFilterableCategories())
            {
                try
                {
                    Category category = Category.GetCategory(document, catId);
                    if (category != null && category.AllowsBoundParameters)
                    {
                        if (category.CategoryType == CategoryType.Model)
                        {
                            output.Add((BuiltInCategory)catId.IntegerValue);
                        }
                    }
                }
                catch (Exception exc)
                {
                    RevitLogger.Error(exc.Message);
                }
            }

            return output;
        }

        #endregion

    }
}
