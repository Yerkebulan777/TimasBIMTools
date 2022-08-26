using Autodesk.Revit.DB;
using RevitTimasBIMTools.RevitModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace RevitTimasBIMTools.RevitUtils
{
    public static class RevitViewModelExtension
    {
        //public static ObservableCollection<RevitElementModel> ToObservableCollection(this FilteredElementCollector collector, string cat = null)
        //{
        //    int idValue;
        //    int invalidId = -1;
        //    ObservableCollection<RevitElementModel> modelList = new ObservableCollection<RevitElementModel>();
        //    foreach (Element elem in collector)
        //    {
        //        idValue = elem.Id.IntegerValue;
        //        if (invalidId != idValue)
        //        {
        //            ElementId typeId = elem.GetTypeId();
        //            if (!invalidId.Equals(typeId))
        //            {
        //                string catName = cat == null ? elem.Category.Name : cat;
        //                ElementType etype = elem.Document.GetElement(typeId) as ElementType;
        //                modelList.Add(new RevitElementModel(idValue, etype.Name, etype.FamilyName, catName));
        //            }
        //        }
        //    };
        //    return modelList;
        //}


        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        {
            return new ObservableCollection<T>(source);
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IList<T> source)
        {
            return new ObservableCollection<T>(source);
        }
    }
}
