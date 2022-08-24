using Autodesk.Revit.DB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Revit.Async;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace RevitTimasBIMTools.ViewModels
{
    public class CutOpeningOptionsViewModel : ObservableObject, IDisposable
    {
        public Document CurrentDocument { get; set; } = null;
        public CutOpeningOptionsViewModel()
        {
        }


        #region Collections

        private int catIdInt = -1;
        public int CategoryIdInt
        {
            get => catIdInt;
            set => SetProperty(ref catIdInt, value);
        }


        private ObservableCollection<Category> catList = new ObservableCollection<Category>();
        public ObservableCollection<Category> RevitCategories
        {
            get => catList;
            set => SetProperty(ref catList, value);
        }


        private ObservableCollection<FamilySymbol> simbols = new ObservableCollection<FamilySymbol>();
        public ObservableCollection<FamilySymbol> RevitFamilySimbols
        {
            get => simbols;
            set => SetProperty(ref simbols, value);
        }

        #endregion


        #region Main Settings Property



        private bool visibility = true;
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

        private int cutOffset = 50;
        public int CutOffset
        {
            get => cutOffset;
            set => SetProperty(ref cutOffset, value);
        }


        private int ratio = 3;
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
                    _ = SetProperty(ref rectangSymbolModel, value);
                    if (rectangSymbolModel is RevitElementModel model)
                    {
                        Properties.Settings.Default.RectangOpeningSimbolIdInt = model.IdInt;
                        Properties.Settings.Default.Save();
                        RevitLogger.Info(model.SymbolName);
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
                    _ = SetProperty(ref roundSymbolModel, value);
                    if (roundSymbolModel is RevitElementModel model)
                    {
                        Properties.Settings.Default.RoundOpeningSimbolIdInt = model.IdInt;
                        Properties.Settings.Default.Save();
                        RevitLogger.Info(model.SymbolName);
                    }
                }
            }
        }

        #endregion


        #region Methods

        public void RaiseExternalEvent()
        {
            if (CurrentDocument != null)
            {
                Document doc = CurrentDocument;
                Task<IList<Category>> catsTask = RevitTask.RaiseGlobal<CutOpeningCategoriesHandler, Document, IList<Category>>(doc);
                Task<IList<FamilySymbol>> simbsTask = RevitTask.RaiseGlobal<CutOpeningFamilyHandler, Document, IList<FamilySymbol>>(doc);
                RevitFamilySimbols = new ObservableCollection<FamilySymbol>(simbsTask.Result);
                RevitCategories = new ObservableCollection<Category>(catsTask.Result);
            }
        }


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