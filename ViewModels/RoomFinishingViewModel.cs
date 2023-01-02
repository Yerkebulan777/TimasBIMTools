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
        }


        private int myVar;
        public int MyProperty
        {
            get { return myVar; }
            set { myVar = value; }
        }


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
                        roomViewData.SortDescriptions.Add(new SortDescription(nameof(Room.Name), ListSortDirection.Ascending));
                        roomViewData.SortDescriptions.Add(new SortDescription(nameof(Room.Number), ListSortDirection.Ascending));
                    }
                }
            }
        }


        public async void GetValidRooms()
        {
            RoomViewCollection = await RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                ElementId paramId = new(BuiltInParameter.ROOM_AREA);
                FilteredElementCollector collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(SpatialElement), BuiltInCategory.OST_Rooms);
                collector = RevitFilterManager.ParamFilterFactory(collector, paramId, 0.5, 1);
                ObservableCollection<Room> collection = new();
                foreach (Room room in collector.ToElements())
                {
                    if (room is not null && 0 < room.Volume)
                    {
                        collection.Add(room);
                    }
                }
                return CollectionViewSource.GetDefaultView(collection);
            });
        }
    }


}
