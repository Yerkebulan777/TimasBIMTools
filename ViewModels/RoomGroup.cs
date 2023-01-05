using Autodesk.Revit.DB.Architecture;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace RevitTimasBIMTools.ViewModels
{
    public sealed class RoomGroup : ObservableObject
    {
        public RoomGroup(string name)
        {
            Group = new ObservableCollection<RoomGroup>();
            Name = name;
        }

        public RoomGroup(Room room)
        {
            Number = room.Number;
            Instance = room;
        }

        private bool selected;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }

        private bool expanded;
        public bool IsExpanded
        {
            get => expanded;
            set => SetProperty(ref expanded, value);
        }


        public string Name { get; set; }
        public string Number { get; set; }
        public Room Instance { get; set; }
        public ObservableCollection<RoomGroup> Group { get; set; }
    }
}
