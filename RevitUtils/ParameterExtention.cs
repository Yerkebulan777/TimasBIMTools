using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GlobalParameter = Autodesk.Revit.DB.GlobalParameter;

namespace RevitTimasBIMTools.RevitUtils
{
    public static class ParameterExtention
    {

        public static string RealString(double arg)
        {
            return arg.ToString("0.##");
        }


        public static string GetParameterType(this Parameter parameter)
        {
            ParameterType prt = parameter.Definition.ParameterType;
            string srp = ParameterType.Invalid == prt ? "" : "/" + prt;
            return parameter.StorageType + srp;
        }


        public static string IsReadWrite(this Parameter parameter)
        {
            return parameter.IsReadOnly ? "Read Only" : "Read Write";
        }


        public static string GetValue(this Parameter _parameter)
        {
            string value;
            switch (_parameter.StorageType)
            {
                // database value, internal revitUnits, e.g. feet:
                case StorageType.Double:
                    value = RealString(_parameter.AsDouble());
                    break;
                case StorageType.Integer:
                    value = _parameter.AsInteger().ToString();
                    break;
                case StorageType.String:
                    value = _parameter.AsString();
                    break;
                case StorageType.ElementId:
                    value = _parameter.AsElementId().IntegerValue.ToString();
                    break;
                case StorageType.None:
                    value = "None";
                    break;
                default:
                    Debug.Assert(false, "unexpected storage type"); value = string.Empty;
                    break;
            }
            return value;
        }

        /// <summary>
        /// SmartToolHelper to return parameter value as string, with additional
        /// support for instance instanceId to display the instance type referred to.
        /// </summary>
        public static string GetParameterValue2(this Parameter param, Document doc)
        {
            if (param.StorageType == StorageType.ElementId)
            {
                ElementId paramId = param.AsElementId();
                Element element = doc.GetElement(paramId);
                return element.Name;
            }
            else
            {
                return GetParameterValue(param);
            }
        }


        /// <summary>
        /// Return a description string for a given instance.
        /// </summary>
        public static string ElementDescription(Element e)
        {
            string description = null == e.Category ? e.GetType().Name : e.Category.Name;

            if (e is FamilyInstance familyInstance)
            {
                description += " '" + familyInstance.Symbol.Family.Name + "'";
            }

            if (null != e.Name)
            {
                description += " '" + e.Name + "'";
            }

            return description;
        }


        /// <summary>
        /// SmartToolHelper to return parameter value as string.
        /// One can also use param.AsValueString() to
        /// get the user interface representation.
        /// </summary>
        public static string GetParameterValue(Parameter param)
        {
            string parameterString = param.StorageType switch
            {
                StorageType.String => param.AsString(),
                StorageType.Double => param.AsValueString(),
                StorageType.Integer => param.AsInteger().ToString(),
                StorageType.ElementId => param.AsElementId().IntegerValue.ToString(),
                _ => throw new NotImplementedException(),
            };
            return parameterString;
        }


        /// <summary>
        /// Return Result of parameter share
        /// </summary>
        /// <param name="parameter">parameter</param>
        /// <returns></returns>
        public static string Shared(this Parameter parameter)
        {
            return parameter.IsShared ? "Shared" : "Non-parameters";
        }


        /// <summary>
        /// Return Guid Of Parameter Share
        /// </summary>
        /// <param name="parameter">parameter</param>
        /// <returns></returns>
        public static string Guid(this Parameter parameter)
        {
            return parameter.IsShared ? parameter.GUID.ToString() : string.Empty;
        }


        /// <summary>
        /// Return Global Parameter SymbolName
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static string GetAssGlobalParameter(this Parameter parameter, Document doc)
        {
            ElementId elementId = parameter.GetAssociatedGlobalParameter();
            if (elementId != null)
            {
                if (doc.GetElement(elementId) is GlobalParameter globalParameter)
                {
                    return globalParameter.GetDefinition().Name;
                }
            }
            return string.Empty;
        }


        /// <summary>
        /// Return Global Parameter Content
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static string GetAssGlobalParameterValue(this Parameter parameter, Document doc)
        {
            ElementId elementId = parameter.GetAssociatedGlobalParameter();
            if (elementId != null)
            {
                if (doc.GetElement(elementId) is GlobalParameter globalParameter)
                {
                    return globalParameter.GetValue() is DoubleParameterValue doublevalue
                        ? RealString(doublevalue.Value)
                        : globalParameter.GetValue() is StringParameterValue strpra ? strpra.Value : string.Empty;
                }
            }
            return string.Empty;
        }


        public static double ConvertFromInternalUnits(double value, string displayUnitType)
        {
            if (string.IsNullOrWhiteSpace(displayUnitType))
            {
                throw new ArgumentNullException(nameof(displayUnitType));
            }

            DisplayUnitType dut = (DisplayUnitType)Enum.Parse(typeof(DisplayUnitType), displayUnitType);
            double result = UnitUtils.ConvertFromInternalUnits(value, dut);

            return result;
        }

        //var prm = SharedParameterElement.Lookup(doc, guid);

    }
}
