using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Reference = Autodesk.Revit.DB.Reference;


namespace RevitTimasBIMTools.RevitSelectionFilter;

internal sealed class SelectionFilterAreaRein : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        if (elem is null)
        {
            return false;
        }

        BuiltInCategory builtInCategory = (BuiltInCategory)GetCategoryIdAsInteger(elem);
        return builtInCategory == BuiltInCategory.OST_AreaRein;
    }
    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }

    private int GetCategoryIdAsInteger(Element element)
    {
        return element?.Category?.Id?.IntegerValue ?? -1;
    }

}