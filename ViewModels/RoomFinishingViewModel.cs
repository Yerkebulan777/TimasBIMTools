using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitUtils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace RevitTimasBIMTools.ViewModels
{
    public sealed class RoomFinishingViewModel : ObservableObject
    {
        public static ExternalEvent RevitExternalEvent { get; set; }
        public RoomFinishingViewModel(APIEventHandler eventHandler)
        {
            RevitExternalEvent = ExternalEvent.Create(eventHandler);
            RoomViewCollection = CollectionViewSource.GetDefaultView(collection);
        }


        private ObservableCollection<Room> collection;
        private ICollectionView roomViewData = null;
        public ICollectionView RoomViewCollection
        {
            get => roomViewData;
            set
            {
                if (SetProperty(ref roomViewData, value))
                {
                    using (roomViewData.DeferRefresh())
                    {
                        roomViewData.SortDescriptions.Clear();
                        roomViewData.GroupDescriptions.Clear();
                        roomViewData.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Room.Name)));
                        roomViewData.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Room.Level.Elevation)));
                        roomViewData.SortDescriptions.Add(new SortDescription(nameof(Room.Level.Elevation), ListSortDirection.Ascending));
                        roomViewData.SortDescriptions.Add(new SortDescription(nameof(Room.Number), ListSortDirection.Ascending));
                        roomViewData.SortDescriptions.Add(new SortDescription(nameof(Room.Name), ListSortDirection.Ascending));
                    }
                }
            }
        }


        public async void GetValidRooms()
        {
            await RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                collection = new ObservableCollection<Room>();
                ElementId paramId = new(BuiltInParameter.ROOM_AREA);
                FilteredElementCollector collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(Room), BuiltInCategory.OST_Rooms);
                collector = RevitFilterManager.ParamFilterFactory(collector, paramId, 0.5, 1);
                foreach (Room room in collector)
                {
                    if (0 < room.Volume)
                    {
                        collection.Add(room);
                    }
                }
            });
        }
    }



}
