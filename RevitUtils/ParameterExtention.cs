using Autodesk.Revit.DB;
using System;
using System.Diagnostics;
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
                    Debug.Assert(false, "unexpected storage stype"); value = string.Empty;
                    break;
            }
            return value;
        }

        /// <summary>
        /// SmartToolHelper to return parameter value as string, with additional
        /// support for instance instanceId to display the instance stype referred to.
        /// </summary>
        public static string GetParameterValueByDocument(this Parameter param, Document doc)
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
        /// SmartToolHelper to return parameter value as string.
        /// One can also use parameter.AsValueString() to
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
        /// Return Guid Of SelectedParameter Share
        /// </summary>
        /// <parameter name="parameter">parameter</parameter>
        /// <returns></returns>
        public static string Guid(this Parameter parameter)
        {
            return parameter.IsShared ? parameter.GUID.ToString() : string.Empty;
        }


        /// <summary>
        /// Return Global SelectedParameter SymbolName
        /// </summary>
        /// <parameter name="parameter"></parameter>
        /// <parameter name="doc"></parameter>
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
        /// Return Global SelectedParameter Content
        /// </summary>
        /// <parameter name="parameter"></parameter>
        /// <parameter name="doc"></parameter>
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


        public static bool SetValue(this Parameter param, object value)
        {
            bool result = false;
            StorageType stype = param.StorageType;
            if (!param.IsReadOnly && value is not null)
            {
                if (stype == StorageType.None)
                {
                    result = false;
                }
                else if (stype == StorageType.String)
                {
                    if (value is string strVal)
                    {
                        result = param.Set(strVal);
                    }
                    else
                    {
                        result = param.Set(Convert.ToString(value));
                    }
                }
                else if (stype == StorageType.Double)
                {
                    if (value is double val)
                    {
                        result = param.Set(val);
                    }
                    else if (value is string strVal)
                    {
                        if (double.TryParse(strVal, out double dblval))
                        {
                            result = param.Set(dblval);
                        }
                    }
                    else
                    {
                        result = param.Set(Convert.ToDouble(value));
                    }
                }
                else if (stype == StorageType.Integer)
                {
                    if (value is int val)
                    {
                        result = param.Set(val);
                    }
                    else if (value is string strVal)
                    {
                        if (int.TryParse(strVal, out int intval))
                        {
                            result = param.Set(intval);
                        }
                    }
                    else
                    {
                        result = param.Set(Convert.ToInt16(value));
                    }
                }
                else if (stype == StorageType.ElementId)
                {
                    if (value is ElementId val)
                    {
                        result = param.Set(val);
                    }
                    else if (value is string strVal)
                    {
                        if (int.TryParse(strVal, out int idval))
                        {
                            result = param.Set(new ElementId(idval));
                        }
                    }
                }
            }
            return result;
        }


        //var prm = SharedParameterElement.Lookup(doc, guid);

    }
}
