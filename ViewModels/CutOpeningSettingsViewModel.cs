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
using System.Windows;
using System.Windows.Input;
using Document = Autodesk.Revit.DB.Document;


namespace RevitTimasBIMTools.ViewModels
{
    public class CutOpeningSettingsViewModel : ObservableObject, IDisposable
    {
        public Window SettingsView { get; set; } = null;
        //private Document currenDocument { get; set; } = null;
        private Categories allCategories { get; set; } = null;
        private FilteredElementCollector collector { get; set; } = null;

        public readonly Dictionary<string, Material> StructuralMaterials = new Dictionary<string, Material>();
        private readonly MaterialFunctionAssignment structure = MaterialFunctionAssignment.Structure;
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
            Task task = CetAllCategoriesAsync();
            if (task.IsCompleted)
            {
                task = GetAllConstructionStructureMaterials();
                if (task.IsCompleted)
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }


        #region General Property

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


        private int commCatIdInt = Properties.Settings.Default.СommunCatIdInt;
        public int СommunCatIdInt
        {
            get => commCatIdInt;
            set
            {
                if (SetProperty(ref commCatIdInt, value))
                {
                    Properties.Settings.Default.СommunCatIdInt = value;
                }
            }
        }


        private string rectangSymbolId = Properties.Settings.Default.RectangSymbolUniqueId;
        public string RectangSymbolUniqueId
        {
            get => rectangSymbolId;
            set
            {
                if (SetProperty(ref rectangSymbolId, value))
                {
                    Properties.Settings.Default.RectangSymbolUniqueId = value;
                }
            }
        }


        private string roundSymbolId = Properties.Settings.Default.RoundSymbolUniqueId;
        public string RoundSymbolUniqueId
        {
            get => roundSymbolId;
            set
            {
                if (SetProperty(ref roundSymbolId, value))
                {
                    Properties.Settings.Default.RoundSymbolUniqueId = value;
                }
            }
        }

        #endregion


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


        //StringFormat={}{0:n5}

        private async Task CetAllCategoriesAsync()
        {
            allCategories = await RevitTask.RunAsync(app =>
            {
                return app.ActiveUIDocument.Document.Settings.Categories;
            });
        }



        private async Task GetAllConstructionStructureMaterials()
        {
            await RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(WallType), BuiltInCategory.OST_Walls);
                foreach (Element elem in collector)
                {
                    Tuple<string, Material> temp = GetStructureMaterial(elem);
                    StructuralMaterials[temp.Item1] = temp.Item2;
                }

                collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(FloorType), BuiltInCategory.OST_Floors);
                foreach (Element elem in collector)
                {
                    Tuple<string, Material> temp = GetStructureMaterial(elem);
                    StructuralMaterials[temp.Item1] = temp.Item2;
                }

                collector = RevitFilterManager.GetInstancesOfCategory(doc, typeof(RoofType), BuiltInCategory.OST_Roofs);
                foreach (Element elem in collector)
                {
                    Tuple<string, Material> temp = GetStructureMaterial(elem);
                    StructuralMaterials[temp.Item1] = temp.Item2;
                }
                collector.Dispose();
            });
        }


        private Tuple<string, Material> GetStructureMaterial(Element elem)
        {
            string name = null;
            Material material = null;
            if (elem is WallType wallType)
            {
                CompoundStructure comStruct = wallType.GetCompoundStructure();
                foreach (CompoundStructureLayer structLayer in comStruct.GetLayers())
                {
                    if (structure == structLayer.Function)
                    {
                        try
                        {
                            material = doc.GetElement(structLayer.MaterialId) as Material;
                            if (null == material)
                            {
                                material = allCategories.get_Item(BuiltInCategory.OST_WallsStructure).Material;
                            }
                        }
                        finally
                        {
                            name = wallType.Name;
                        }
                        break;
                    }
                }
            }
            else if (elem is FloorType floorType)
            {
                CompoundStructure comStruct = floorType.GetCompoundStructure();
                foreach (CompoundStructureLayer structLayer in comStruct.GetLayers())
                {
                    if (structure == structLayer.Function)
                    {
                        try
                        {
                            material = doc.GetElement(structLayer.MaterialId) as Material;
                            if (null == material)
                            {
                                material = allCategories.get_Item(BuiltInCategory.OST_FloorsStructure).Material;
                            }
                        }
                        finally
                        {
                            name = floorType.Name;
                        }
                        break;
                    }
                }
            }
            else if (elem is RoofType roofType)
            {
                CompoundStructure comStruct = roofType.GetCompoundStructure();
                foreach (CompoundStructureLayer structLayer in comStruct.GetLayers())
                {
                    if (structure == structLayer.Function)
                    {
                        try
                        {
                            material = doc.GetElement(structLayer.MaterialId) as Material;
                            if (null == material)
                            {
                                material = allCategories.get_Item(BuiltInCategory.OST_RoofsStructure).Material;
                            }
                        }
                        finally
                        {
                            name = roofType.Name;
                        }
                        break;
                    }
                }
            }
            return Tuple.Create(name, material);
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}