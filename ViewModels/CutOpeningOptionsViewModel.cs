using Autodesk.Revit.DB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Revit.Async;
using RevitTimasBIMTools.RevitUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;



namespace RevitTimasBIMTools.ViewModels
{
    public class CutOpeningOptionsViewModel : ObservableObject, IDisposable
    {
        private readonly IList<BuiltInCategory> builtInCats = new List<BuiltInCategory>
        {
            BuiltInCategory.OST_Conduit,
            BuiltInCategory.OST_CableTray,
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_GenericModel,
            BuiltInCategory.OST_MechanicalEquipment
        };


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


        #region Main Settings Property

        private Document doc = null;
        public Document CurrentDocument
        {
            get => doc;
            set
            {
                if (value != null)
                {
                    doc = value;
                    OnPropertyChanged(nameof(CurrentDocument));
                    CommandManager.InvalidateRequerySuggested();
                };
            }
        }


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

        private int cutOffset = Properties.Settings.Default.CutOffsetInMm;
        public int CutOffset
        {
            get => cutOffset;
            set
            {
                SetProperty(ref cutOffset, value);
                Properties.Settings.Default.CutOffsetInMm = cutOffset;
            }
        }


        private int ratio = 3;
        public int RatioLimit
        {
            get => ratio;
            set => SetProperty(ref ratio, value);
        }

        #endregion


        #region FamilySimbol Property

        private FamilySymbol rectangSymbolModel = null;
        public FamilySymbol RectangSimbolModel
        {
            get => rectangSymbolModel;
            set
            {
                if (value != null)
                {
                    SetProperty(ref rectangSymbolModel, value);
                    if (rectangSymbolModel is FamilySymbol fam)
                    {
                        Properties.Settings.Default.RectanOpeningSimbolIdInt = fam.Id.IntegerValue;
                    }
                }
            }
        }

        private FamilySymbol roundSymbolModel = null;
        public FamilySymbol RoundSimbolModel
        {
            get => roundSymbolModel;
            set
            {
                if (value != null)
                {
                    SetProperty(ref roundSymbolModel, value);
                    if (roundSymbolModel is FamilySymbol fam)
                    {
                        Properties.Settings.Default.RoundOpeningSimbolIdInt = fam.Id.IntegerValue;
                    }
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