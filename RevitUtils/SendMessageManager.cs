using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Diagnostics;

namespace RevitTimasBIMTools.RevitUtils
{
    public class SendMessageManager
    {
        private const string caption = "Timas BIM Tools";
        public static void InfoMsg(string instruction)
        {
            string contentInfo = "Information";
            Debug.WriteLine($"{instruction}\r\n{contentInfo}");
            TaskDialog d = new TaskDialog(caption)
            {
                MainInstruction = instruction,
                MainContent = contentInfo
            };
            d.Show();
        }

        public static void ErrorMsg(string instruction)
        {
            string contentError = "WARNING";
            Debug.WriteLine($"{instruction}\r\n{contentError}");
            TaskDialog d = new TaskDialog(caption)
            {
                MainInstruction = instruction,
                MainContent = contentError
            };
            d.Show();
        }

        // For a wall, the instance name equals the wall type name, which is equivalent to the family name ...
        public static string ElementDescription(Element elem)
        {
            if (elem.IsValidObject && elem is FamilyInstance)
            {
                FamilyInstance finst = elem as FamilyInstance;

                string typeName = elem.GetType().Name;
                string famName = null == finst ? string.Empty : $"{finst.Symbol.Family.Name}";
                string catName = null == elem.Category ? string.Empty : $"{elem.Category.Name}";
                string symbName = null == finst || elem.Name.Equals(finst.Symbol.Name) ? string.Empty : $"{finst.Symbol.Name}";

                return $"{famName}-{symbName}<{elem.Id.IntegerValue} {elem.Name}>({typeName}-{catName})";
            }
            return "<null>";
        }


        /// <summary>
        /// Возвращает точку расположения экземпляра семейства или значение null
        /// </summary>
        public static XYZ GetFamilyInstanceLocation(FamilyInstance fi)
        {
            return ((LocationPoint)fi?.Location)?.Point;
        }
    }
}
