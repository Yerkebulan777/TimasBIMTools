﻿using Autodesk.Revit.DB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Revit.Async;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Document = Autodesk.Revit.DB.Document;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutOpeningSettingsViewModel : ObservableObject, IDisposable
    {
        public Window SettingsView { get; set; } = null;
        //private Document currenDocument { get; set; } = null;

        private FilteredElementCollector collector { get; set; } = null;
        public Dictionary<string, string> StructuralMaterials = new Dictionary<string, string>();

        private readonly IList<BuiltInCategory> builtInCats = new List<BuiltInCategory>
        {
            BuiltInCategory.OST_Conduit,
            BuiltInCategory.OST_CableTray,
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_GenericModel,
            BuiltInCategory.OST_MechanicalEquipment
        };


        public CutOpeningSettingsViewModel()
        {
        }



        #region Size Property

        private int minSize = Properties.Settings.Default.MinSideSize;
        public int MinElementSize
        {
            get => minSize;
            set
            {
                value = NormilizeIntValue(value, 5, 100);
                if (SetProperty(ref minSize, value))
                {
                    Properties.Settings.Default.MinSideSize = minSize;
                }
            }
        }


        private int maxSize = Properties.Settings.Default.MaxSideSize;
        public int MaxElementSize
        {
            get => maxSize;
            set
            {
                value = NormilizeIntValue(value, 100, 1500);
                if (SetProperty(ref maxSize, value))
                {
                    Properties.Settings.Default.MaxSideSize = maxSize;
                }
            }
        }

        #endregion


        #region Opening Property

        private int cutOffset = Properties.Settings.Default.CutOffsetInMm;
        public int CutOffset
        {
            get => cutOffset;
            set
            {
                value = NormilizeIntValue(value, 0, 150);
                if (SetProperty(ref cutOffset, value))
                {
                    Properties.Settings.Default.CutOffsetInMm = cutOffset;
                }
            }
        }


        private int ratio = Properties.Settings.Default.Ratio;
        public int Ratio
        {
            get => ratio;
            set
            {
                value = NormilizeIntValue(value, 1, 5);
                if (SetProperty(ref ratio, value))
                {
                    Properties.Settings.Default.Ratio = ratio;
                }
            }
        }

        #endregion


        #region Methods
        public async Task RaiseExternalEventAsync()
        {
            await GetTargetCategories();
            await GetOpeningFamilySymbols();
        }

        private async Task GetTargetCategories()
        {
            RevitCategories?.Clear();
            RevitCategories = await RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                IList<Category> output = GetCategories(doc, builtInCats);
                return new ObservableCollection<Category>(output.OrderBy(i => i.Name).ToList());
            });
        }

        private async Task GetOpeningFamilySymbols()
        {
            RevitFamilySimbols?.Clear();
            RevitFamilySimbols = await RevitTask.RunAsync(app =>
            {
                FilteredElementCollector collector;
                Document doc = app.ActiveUIDocument.Document;
                IList<FamilySymbol> output = new List<FamilySymbol>();
                BuiltInCategory bic = BuiltInCategory.OST_GenericModel;
                collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(FamilySymbol), bic);
                foreach (FamilySymbol symbol in collector)
                {
                    Family family = symbol.Family;
                    if (family.IsValidObject && family.IsEditable)
                    {
                        if (family.FamilyPlacementType.Equals(FamilyPlacementType.OneLevelBasedHosted))
                        {
                            output.Add(symbol);
                        }
                    }
                }
                return new ObservableCollection<FamilySymbol>(output.OrderBy(i => i.Name).ToList());
            });
        }


        private IList<Category> GetCategories(Document doc, IList<BuiltInCategory> bics)
        {
            IList<Category> output = new List<Category>();
            foreach (BuiltInCategory catId in bics)
            {
                Category cat = null;
                try
                {
                    cat = Category.GetCategory(doc, catId);
                }
                finally
                {
                    if (cat != null)
                    {
                        output.Add(cat);
                    }
                }
            }
            return output;
        }


        private static int NormilizeIntValue(int value, int minVal = 0, int maxVal = 100)
        {
            if (value < minVal)
            {
                value = minVal;
            }
            if (value > maxVal)
            {
                value = maxVal;
            }
            return value;
        }

        #endregion


        #region ObservableCollection

        private ObservableCollection<Category> catList = null;
        public ObservableCollection<Category> RevitCategories
        {
            get => catList;
            set => SetProperty(ref catList, value);
        }


        private ObservableCollection<FamilySymbol> simbols = null;
        public ObservableCollection<FamilySymbol> RevitFamilySimbols
        {
            get => simbols;
            set => SetProperty(ref simbols, value);
        }

        #endregion


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}