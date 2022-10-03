using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace RevitTimasBIMTools.RevitUtils
{
    public static class ViewModelExtension
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        {
            return source != null ? new ObservableCollection<T>(source) : new ObservableCollection<T>();
        }


        public static ObservableCollection<T> ToObservableCollection<T>(this IList<T> source)
        {
            return source != null ? new ObservableCollection<T>(source) : new ObservableCollection<T>();
        }

    }
}
