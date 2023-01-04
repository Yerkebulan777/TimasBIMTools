using Autodesk.Revit.DB.Architecture;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class RoomGroup : ObservableObject
    {
        public RoomGroup(string name, List<Room> rooms)
        {
            Rooms = new ObservableCollection<Room>(rooms);
            Name = name;
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
        public ObservableCollection<Room> Rooms { get; set; }
    }
}
