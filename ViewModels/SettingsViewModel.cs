using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Revit.Async;
using Revit.Async.ExternalEvents;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace RevitTimasBIMTools.ViewModels
{
    public class SettingsViewModel : ObservableObject, IDisposable
    {
        private Document document { get; set; }

        public SettingsViewModel()
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
            set
            {
                if (value != null)
                {
                    SetProperty(ref catList, value);
                    RevitLogger.Info($"{catList.Count}");
                }
            }
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

        private string RaiseExternalEvent()
        {
            return RevitTask.RaiseGlobal<TestExternalEventHandler, Document, string>(document).Result;
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

    public class TestExternalEventHandler : SyncGenericExternalEventHandler<Document, string>
    {
        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public override string GetName()
        {
            return "TestExternalEventHandler";
        }

        protected override string Handle(UIApplication app, Document parameter)
        {
            //write sync logic here
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string path = Path.Combine(desktop, $"{parameter.Title}");
            return path;
        }
    }
}