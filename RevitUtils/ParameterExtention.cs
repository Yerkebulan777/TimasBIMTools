using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GlobalParameter = Autodesk.Revit.DB.GlobalParameter;

namespace RevitTimasBIMTools.RevitUtils
{
    public static class ParameterExtention
    {
        public const string Caption = "BIM Tools";
        public static List<Element> GetSelection(this UIDocument uidoc)
        {
            Document doc = uidoc.Document;
            uidoc.Selection.GetElementIds();
            Type stp = uidoc.Selection.GetType();
            List<Element> value = new List<Element>();
            if (stp.GetMethod("GetElementIds") != null)
            {
                MethodInfo met = stp.GetMethod("GetElementIds");
                value = ((ICollection<ElementId>)met.Invoke(uidoc.Selection, null)).Select(a => doc.GetElement(a)).ToList();
            }
            else
            {
                value = ((System.Collections.IEnumerable)stp.GetProperty("ElementModelData").GetValue(uidoc.Selection, null)).Cast<Element>().ToList();
            }
            return value.OrderBy(x => x.Name).ToList();
        }

        /// <summary>
        /// MessageBox wrapper for question message.
        /// </summary>
        public static bool QuestionMsg(string msg)
        {
            Debug.WriteLine(msg);

            TaskDialog dialog = new TaskDialog(Caption)
            {
                MainIcon = TaskDialogIcon.TaskDialogIconNone,
                MainInstruction = msg
            };

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "instance parameters");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Type parameters");

            return dialog.Show() == TaskDialogResult.CommandLink1;
        }

        /// <summary>
        /// Return Real String Of Double
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
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
            if (param.StorageType == StorageType.ElementId && doc != null)
            {
                ElementId paramId = param.AsElementId();
                int id = paramId.IntegerValue;

                if (id < 0)
                {
                    return id.ToString() + BuiltInCategoryString(id);
                }
                else
                {
                    Element element = doc.GetElement(paramId);

                    return ElementDescription(element, true);
                }
            }
            else
            {
                return GetParameterValue(param);
            }
        }

        private static int _min_bic = 0;
        private static int _max_bic = 0;

        private static string BuiltInCategoryString(int id)
        {
            if (_min_bic == 0)
            {
                SetMinAndMaxBuiltInCategory();
            }

            return _min_bic < id && id < _max_bic ? " " + ((BuiltInCategory)id).ToString() : string.Empty;
        }

        private static void SetMinAndMaxBuiltInCategory()
        {
            Array values = Enum.GetValues(typeof(BuiltInCategory));
            _max_bic = values.Cast<int>().Max();
            _min_bic = values.Cast<int>().Min();
        }
        /// <summary>
        /// Return a description string including instance instanceId for a given instance.
        /// </summary>
        public static string ElementDescription(Element element, bool includeId)
        {
            string description = ElementDescription(element);

            if (includeId)
            {
                description += " " + element.Id.IntegerValue.ToString();
            }

            return description;
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
            string parameterString;
            switch (param.StorageType)
            {
                case StorageType.Double:
                    parameterString = param.AsValueString();
                    break;
                case StorageType.Integer:
                    parameterString = param.AsInteger().ToString();
                    break;
                case StorageType.String:
                    parameterString = param.AsString();
                    break;
                case StorageType.ElementId:
                    parameterString = param.AsElementId().IntegerValue.ToString();
                    break;
                case StorageType.None:
                    parameterString = "?NONE?";
                    break;
                default:
                    parameterString = "?ELSE?";
                    break;
            }
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
            Dictionary<string, string> gloDictionary = new Dictionary<string, string>();
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
            Dictionary<string, string> gloDictionary = new Dictionary<string, string>();
            ElementId elementId = parameter.GetAssociatedGlobalParameter();
            if (elementId != null)
            {
                if (doc.GetElement(elementId) is GlobalParameter globalParameter)
                {
                    DoubleParameterValue doublevalue = globalParameter.GetValue() as DoubleParameterValue;
                    StringParameterValue strpra = globalParameter.GetValue() as StringParameterValue;
                    if (doublevalue != null)
                    {
                        return RealString(doublevalue.Value);
                    }
                    if (strpra != null)
                    {
                        return strpra.Value;
                    }
                    return string.Empty;

                }
            }
            return string.Empty;
        }

    }
}
