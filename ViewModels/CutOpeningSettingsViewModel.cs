using Autodesk.Revit.DB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Windows;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutOpeningSettingsViewModel : ObservableObject, IDisposable
    {
        public Window SettingsView { get; set; } = null;
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