using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows.Data;

namespace RevitTimasBIMTools.ViewModels
{
    internal sealed class RoomFinishingViewModel : ObservableObject
    {

        private ListCollectionView roomData;
        public ListCollectionView RoomCollection
        {
            get => roomData;
            set
            {
                if (SetProperty(ref roomData, value))
                {
                    using (roomData.DeferRefresh())
                    {
                        roomData.SortDescriptions.Clear();
                        roomData.GroupDescriptions.Clear();
                        roomData.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Room.Name)));
                        roomData.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Room.Level.Elevation)));
                        roomData.SortDescriptions.Add(new SortDescription(nameof(Room.Level.Elevation), ListSortDirection.Ascending));
                        roomData.SortDescriptions.Add(new SortDescription(nameof(Room.Number), ListSortDirection.Ascending));
                        roomData.SortDescriptions.Add(new SortDescription(nameof(Room.Name), ListSortDirection.Ascending));
                    }
                }
            }
        }


    }
}
