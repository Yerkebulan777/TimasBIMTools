using RevitTimasBIMTools.ViewModels;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace RevitTimasBIMTools.ViewConverters
{

    internal class RowFromViewItem : IValueConverter
    {
        private readonly CutOpeningDataViewModel dataViewModel = ViewModelLocator.DataViewModel;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollectionView collection = dataViewModel.ItemCollectionView;
            return !collection.IsEmpty && collection is CollectionView collectionView ? 1 + collectionView.IndexOf(value) : (object)null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 1;
        }
    }
}
