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
using System.Linq;
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

        private ICollectionView roomView = null;
        public ICollectionView RoomCollectionView
        {
            get => roomView;
            set
            {
                if (SetProperty(ref roomView, value))
                {
                    using (roomView.DeferRefresh())
                    {
                        roomView.SortDescriptions.Clear();
                        roomView.GroupDescriptions.Clear();
                        roomView.SortDescriptions.Add(new SortDescription(nameof(RoomGroup.Name), ListSortDirection.Ascending));
                    }
                }
            }
        }


        public async void GetValidRooms()
        {
            RoomCollectionView = await RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                BuiltInCategory bip = BuiltInCategory.OST_Rooms;
                ElementId paramId = new(BuiltInParameter.ROOM_AREA);
                ObservableCollection<RoomGroup> roomGroupData = new();
                FilteredElementCollector collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(SpatialElement), bip);
                foreach (Room room in RevitFilterManager.ParamFilterFactory(collector, paramId, 0.5, 1))
                {
                    string name = room.Name;
                    double volume = room.Volume;
                    Location location = room.Location;
                    if (location is not null && 0 < volume)
                    {
                        RoomGroup group = roomGroupData.FirstOrDefault(s => s.Name == name);
                        if (group is not null and RoomGroup roomGroup)
                        {
                            if (roomGroupData.Remove(group))
                            {
                                roomGroup.Rooms.Add(room);
                                roomGroupData.Add(roomGroup);
                            }
                        }
                        else
                        {
                            List<Room> rooms = new() { room };
                            roomGroupData.Add(new RoomGroup(name, rooms));
                        }
                    }
                }
                return CollectionViewSource.GetDefaultView(roomGroupData);
            });
        }
    }


}
