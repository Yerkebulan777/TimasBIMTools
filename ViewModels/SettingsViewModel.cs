﻿using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.ObjectModel;

namespace RevitTimasBIMTools.ViewModels
{
    public class SettingsViewModel : ObservableObject, IDisposable
    {
        private ObservableCollection<RevitElementModel> simbolList = new ObservableCollection<RevitElementModel>();
        public ObservableCollection<RevitElementModel> SimbolList
        {
            get => simbolList;
            set => SetProperty(ref simbolList, value);
        }


        #region Main Settings Property

        private bool visibility = false;
        public bool AdvancedSettingsVisibility
        { 
            get => visibility;
            set => SetProperty(ref visibility, value);
        } 

        public bool SetApply { get; private set; } = false;

        #endregion


        #region Communication Element Property

        private int minElementHight = 30;
        public int MinElementHight
        {
            get => minElementHight;
            set
            {
                if (value != minElementHight)
                {
                    minElementHight = NormilizeIntValue(value, 100, 0);
                    OnPropertyChanged(nameof(MinElementHight));
                }
            }
        }


        private int minElementWidth = 30;
        public int MinElementWidth
        {
            get => minElementWidth;
            set
            {
                if (value != minElementWidth)
                {
                    minElementWidth = NormilizeIntValue(value, 100, 0);
                    OnPropertyChanged(nameof(MinElementWidth));
                }
            }
        }

        private int maxElementHight = 500;
        public int MaxElementHight
        {
            get => maxElementHight;
            set
            {
                if (value != maxElementHight)
                {
                    maxElementHight = NormilizeIntValue(value, 1500, 100);
                    OnPropertyChanged(nameof(MaxElementHight));
                }
            }
        }

        private int maxElementWidht = 500;
        public int MaxElementWidth
        {
            get => maxElementWidht;
            set
            {
                if (value != maxElementWidht)
                {
                    maxElementWidht = NormilizeIntValue(value, 1500, 100);
                    OnPropertyChanged(nameof(MaxElementWidth));
                }
            }
        }

        #endregion


        #region Create Opening Property

        private int catIdInt = -1;
        public int CategoryIdInt
        {
            get => catIdInt;
            set => SetProperty(ref catIdInt, value);
        }


        private int cutOffset = 50;
        public int CutOffset
        {
            get => cutOffset;
            set => SetProperty(ref cutOffset, value);
        }


        private int ratio = 5;
        public int RatioLimit
        {
            get => ratio;
            set => SetProperty(ref ratio, value);
        }

        #endregion


        #region FamilySimbol Property

        private RevitElementModel rectangSymbolModel = null;
        public RevitElementModel RectangSimbolModel
        {
            get => rectangSymbolModel;
            set
            {
                if (value != null)
                {
                    SetProperty(ref rectangSymbolModel, value);
                    if (rectangSymbolModel is RevitElementModel model)
                    {
                        Properties.Settings.Default.RectangOpeningSimbolIdInt = model.IdInt;
                        Properties.Settings.Default.Save();
                        LogManager.Info(model.SymbolName);
                    }
                }
            }
        }

        private RevitElementModel roundSymbolModel = null;



        public RevitElementModel RoundSimbolModel
        {
            get => roundSymbolModel;
            set
            {
                if (value != null)
                {
                    SetProperty(ref roundSymbolModel, value);
                    if (roundSymbolModel is RevitElementModel model)
                    {
                        Properties.Settings.Default.RoundOpeningSimbolIdInt = model.IdInt;
                        Properties.Settings.Default.Save();
                        LogManager.Info(model.SymbolName);
                    }
                }
            }
        }

        #endregion


        #region Methods

        private static int NormilizeIntValue(int value, int maxVal = 100, int minVal = 0)
        {
            if (value > maxVal)
            {
                value = maxVal;
            }
            if (value < minVal)
            {
                value = minVal;
            }
            return value;
        }

        #endregion


        //StringFormat={}{0:n5}


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}