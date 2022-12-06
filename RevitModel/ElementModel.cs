﻿using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RevitTimasBIMTools.RevitModel
{
    public sealed class ElementModel : ObservableObject
    {
        public readonly Element Host;
        public readonly Element Instanse;
        public string LevelName { get; private set; }
        public string SymbolName { get; private set; }
        public string FamilyName { get; private set; }

        public ElementModel(Element instanse, Element host)
        {
            Element etype = instanse.Document.GetElement(instanse.GetTypeId());
            Level level = host.Document.GetElement(host.LevelId) as Level;
            if (etype.IsValidObject && etype is ElementType elementType)
            {
                Host = host;
                Instanse = instanse;
                LevelName = level?.Name;
                SymbolName = elementType.Name;
                FamilyName = elementType.FamilyName;
            }
        }


        public Plane SectionPlane { get; internal set; }
        public BoundingBoxUV SectionBox { get; internal set; }
        public int HostCategoryIntId { get; internal set; }
        public string Description { get; internal set; }
        public int SizeInMm { get; internal set; }
        public double Height { get; internal set; }
        public double Width { get; internal set; }
        public double Depth { get; internal set; }

        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }


        public bool IsValidModel()
        {
            return Instanse != null && Instanse.IsValidObject && IsSelected;
        }


        public void SetSizeDescription()
        {
            int h = Convert.ToInt16(Height * 304.8);
            int w = Convert.ToInt16(Width * 304.8);
            Description = $"{w}x{h}(h)";
            SizeInMm = h + w;
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }

    }
}
