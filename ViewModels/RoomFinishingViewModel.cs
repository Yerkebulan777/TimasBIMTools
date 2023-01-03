using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.RevitUtils;
using System.Collections.Generic;
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

        private SortedList<string, ObservableCollection<Room>> roomData { get; set; }
        private ICollectionView roomViewData = null;
        public ICollectionView RoomCollectionView
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
                        roomViewData.SortDescriptions.Add(new SortDescription("Key", ListSortDirection.Ascending));
                        roomViewData.SortDescriptions.Add(new SortDescription("Value.Number", ListSortDirection.Ascending));
                    }
                }
            }
        }


        public async void GetValidRooms()
        {
            RoomCollectionView = await RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                ElementId paramId = new(BuiltInParameter.ROOM_AREA);
                roomData = new SortedList<string, ObservableCollection<Room>>();
                FilteredElementCollector collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(SpatialElement), BuiltInCategory.OST_Rooms);
                collector = RevitFilterManager.ParamFilterFactory(collector, paramId, 0.5, 1);
                foreach (Room room in collector.ToElements())
                {
                    string name = room.Name;
                    double volume = room.Volume;
                    Location location = room.Location;
                    if (location is not null && 0 < volume)
                    {
                        if (roomData.TryGetValue(name, out ObservableCollection<Room> data))
                        {
                            data.Add(room);
                            roomData[name] = data;
                        }
                        else
                        {
                            roomData.Add(name, new ObservableCollection<Room> { room });
                        }
                    }
                }
                return CollectionViewSource.GetDefaultView(roomData);
            });
        }
    }


}
