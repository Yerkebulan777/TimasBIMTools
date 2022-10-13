using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using RevitTimasBIMTools.Core;
using RevitTimasBIMTools.CutOpening;
using RevitTimasBIMTools.RevitModel;
using RevitTimasBIMTools.RevitUtils;
using RevitTimasBIMTools.Services;
using RevitTimasBIMTools.Views;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Document = Autodesk.Revit.DB.Document;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutVoidDataViewModel : ObservableObject, IDisposable
    {
        public CutVoidDockPanelView DockPanelView { get; set; } = null;

        private Document doc { get; set; } = null;
        private View3D view3d { get; set; } = null;
        private ConcurrentQueue<Element> instances { get; set; } = null;
        private CancellationToken cancelToken { get; set; } = CancellationToken.None;
        private string documentId { get; } = Properties.Settings.Default.ActiveDocumentUniqueId;
        private CutVoidCollisionManager manager { get; set; } = SmartToolApp.ServiceProvider.GetRequiredService<CutVoidCollisionManager>();


        private readonly TaskScheduler taskContext = CustomSynchronizationContext.GetSynchronizationContext();
        private readonly SynchronizationContext syncContext = SynchronizationContext.Current;


        public CutVoidDataViewModel()
        {
            SettingsCommand = new RelayCommand(SettingsHandelCommand);
            ShowExecuteCommand = new AsyncRelayCommand(ExecuteHandelCommandAsync);
            SelectItemCommand = new RelayCommand(SelectAllVaueHandelCommand);
            CanselCommand = new RelayCommand(CancelCallbackLogic);
        }


        #region Visibility

        private bool started = false;
        public bool IsStarted
        {
            get => started;
            set
            {
                if (SetProperty(ref started, value))
                {
                    GetGeneral3DViewAsync();
                }
            }
        }


        private bool enableOpt = false;
        public bool IsOptionEnabled
        {
            get => enableOpt;
            set
            {
                if (!string.IsNullOrEmpty(documentId))
                {
                    if (SetProperty(ref enableOpt, value))
                    {
                        ClearElementDataAsync();
                        SetMEPCategoriesToData();
                        SetCoreMaterialsToData();
                        SetFamilySymbolsToData();
                    }
                }
            }
        }


        private bool enableData = false;
        public bool IsDataEnabled
        {
            get => enableData;
            set
            {
                if (docModel != null && category != null)
                {
                    if (SetProperty(ref enableData, value))
                    {
                        IsOptionEnabled = !enableData;
                        DataViewCollection?.Refresh();
                        SetValidLevelsToData();
                    }
                }
            }
        }

        #endregion


        #region Settings

        private ObservableCollection<DocumentModel> docModels = null;
        public ObservableCollection<DocumentModel> DocumentModelCollection
        {
            get => docModels;
            set
            {
                if (value != null && SetProperty(ref docModels, value))
                {
                    SelectedDocModel = docModels.FirstOrDefault();
                }
            }
        }


        private IDictionary<int, ElementId> constructions = null;
        public IDictionary<int, ElementId> ConstructionTypeIds
        {
            get => constructions;
            set
            {
                if (value != null && SetProperty(ref constructions, value))
                {
                    Logger.Log(constructions.Count.ToString());
                }
            }
        }


        private IDictionary<string, Category> categories = null;
        public IDictionary<string, Category> EngineerCategories
        {
            get => categories;
            set
            {
                if (value != null && SetProperty(ref categories, value))
                {
                    Logger.Log(categories.Count.ToString());
                }
            }
        }


        private IDictionary<string, Material> structMats = null;
        public IDictionary<string, Material> StructureMaterials
        {
            get => structMats;
            set
            {
                if (value != null && SetProperty(ref structMats, value))
                {
                    Logger.Log(structMats.Count.ToString());
                }
            }
        }


        private IDictionary<string, FamilySymbol> symbols = null;
        public IDictionary<string, FamilySymbol> FamilySymbols
        {
            get => symbols;
            set
            {
                if (value != null && SetProperty(ref symbols, value))
                {
                    Logger.Log(nameof(FamilySymbols));
                }
            }
        }


        private DocumentModel docModel = null;
        public DocumentModel SelectedDocModel
        {
            get => docModel;
            set
            {
                if (value != null && SetProperty(ref docModel, value))
                {
                    manager.SearchDoc = docModel.Document;
                    manager.SearchGlobal = docModel.Transform;
                    manager.SearchInstance = docModel.LinkInstance;
                }
            }
        }


        private Category category = null;
        public Category SelectedCategory
        {
            get => category;
            set
            {
                if (value != null && SetProperty(ref category, value))
                {
                    manager.SearchCatId = category.Id;
                }
            }
        }


        private Material material = null;
        public Material SelectedMaterial
        {
            get => material;
            set
            {
                if (value != null && SetProperty(ref material, value))
                {
                    GetInstancesByCoreMaterialInType(material.Name);
                }
            }
        }


        private FamilySymbol rectang = null;
        public FamilySymbol RectangSymbol
        {
            get => rectang;
            set
            {
                if (value != null && SetProperty(ref rectang, value))
                {
                    Properties.Settings.Default.RectangSymbol = rectang.UniqueId;
                    Properties.Settings.Default.Save();
                    ActivateFamilySimbolAsync(rectang);
                }
            }
        }


        private FamilySymbol rounded = null;
        public FamilySymbol RoundedSymbol
        {
            get => rounded;
            set
            {
                if (value != null && SetProperty(ref rounded, value))
                {
                    Properties.Settings.Default.RoundedSymbol = rounded.UniqueId;
                    Properties.Settings.Default.Save();
                    ActivateFamilySimbolAsync(rounded);
                }
            }
        }


        private int minSize = Properties.Settings.Default.MinSideSizeInMm;
        public int MinSideSize
        {
            get => minSize;
            set
            {
                if (SetProperty(ref minSize, value))
                {
                    Properties.Settings.Default.MinSideSizeInMm = minSize;
                    Properties.Settings.Default.Save();
                    ResetCurrentContext();
                }
            }
        }


        private int maxSize = Properties.Settings.Default.MaxSideSizeInMm;
        public int MaxSideSize
        {
            get => maxSize;
            set
            {
                if (SetProperty(ref maxSize, value))
                {
                    Properties.Settings.Default.MaxSideSizeInMm = minSize;
                    Properties.Settings.Default.Save();
                    ResetCurrentContext();
                }
            }
        }


        private int cutOffset = Properties.Settings.Default.CutOffsetInMm;
        public int CutOffsetSize
        {
            get => cutOffset;
            set
            {
                if (SetProperty(ref cutOffset, value))
                {
                    Properties.Settings.Default.CutOffsetInMm = minSize;
                    Properties.Settings.Default.Save();
                    ResetCurrentContext();
                }
            }
        }

        #endregion


        #region Methods

        private void ResetCurrentContext()
        {
            if (SynchronizationContext.Current != syncContext)
            {
                try
                {
                    Logger.Log(nameof(ResetCurrentContext));
                    SynchronizationContext.SetSynchronizationContext(syncContext);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            }
        }


        private async void ClearElementDataAsync()
        {
            if (IsOptionEnabled || IsDataEnabled)
            {
                Properties.Settings.Default.Reset();
                await Task.Delay(1000).ContinueWith(_ =>
                {
                    ElementModelData = new ObservableCollection<ElementModel>();
                    EngineerCategories = new Dictionary<string, Category>();
                    StructureMaterials = new Dictionary<string, Material>();
                    FamilySymbols = new Dictionary<string, FamilySymbol>();

                }, taskContext);
            }
        }


        private async void GetGeneral3DViewAsync()
        {
            ResetCurrentContext();
            view3d = await RevitTask.RunAsync(app =>
            {
                return RevitViewManager.Get3dView(app.ActiveUIDocument);
            });
        }


        private async void SetMEPCategoriesToData()
        {
            ResetCurrentContext();
            EngineerCategories = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return documentId.Equals(doc.ProjectInformation.UniqueId) ? RevitFilterManager.GetEngineerCategories(doc) : null;
            });
        }


        private async void SetCoreMaterialsToData()
        {
            ResetCurrentContext();
            StructureMaterials = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return documentId.Equals(doc.ProjectInformation.UniqueId) ? RevitFilterManager.GetConstructionCoreMaterials(doc, ConstructionTypeIds) : null;
            });
        }


        private async void SetFamilySymbolsToData()
        {
            ResetCurrentContext();
            FamilySymbols = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return documentId.Equals(doc.ProjectInformation.UniqueId)
                    ? RevitFilterManager.GetHostedFamilySymbols(doc, BuiltInCategory.OST_GenericModel) : null;
            });
        }


        private async void SetValidLevelsToData()
        {
            ResetCurrentContext();
            ValidLevels = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return documentId.Equals(doc.ProjectInformation.UniqueId) ? RevitFilterManager.GetValidLevels(doc) : null;
            });
        }


        private async void GetInstancesByCoreMaterialInType(string matName)
        {
            ResetCurrentContext();
            instances = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return documentId.Equals(doc.ProjectInformation.UniqueId)
                    ? RevitFilterManager.GetInstancesByCoreMaterial(doc, constructions, matName) : null;
            });
        }


        private async void SnoopIntersectionDataByLevel(Level level)
        {
            ResetCurrentContext();
            ElementModelData = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return documentId.Equals(doc.ProjectInformation.UniqueId)
                    ? manager.GetCollisionByLevel(doc, level, instances).ToObservableCollection() : null;
            });
        }


        private async void ActivateFamilySimbolAsync(FamilySymbol symbol)
        {
            await RevitTask.RunAsync(app =>
            {
                if (symbol != null && !symbol.IsActive)
                {
                    symbol.Activate();
                }
            });
        }

        #endregion


        #region DataGrid

        private bool? isSelected = false;
        public bool? IsAllSelectChecked
        {
            get => isSelected;
            set
            {
                if (SetProperty(ref isSelected, value))
                {
                    if (value.HasValue)
                    {
                        bool val = (bool)value;
                        foreach (ElementModel model in DataViewCollection)
                        {
                            model.IsSelected = val;
                        }
                    }

                }
            }
        }


        private ObservableCollection<ElementModel> dataModels = new();
        public ObservableCollection<ElementModel> ElementModelData
        {
            get => dataModels;
            set
            {
                if (SetProperty(ref dataModels, value))
                {
                    DataViewCollection = CollectionViewSource.GetDefaultView(value);
                    UniqueItemNames = GetUniqueStringList(value);
                    DataViewCollection.Refresh();
                }
            }
        }


        private ICollectionView dataView = null;
        public ICollectionView DataViewCollection
        {
            get => dataView;
            set
            {
                if (SetProperty(ref dataView, value))
                {
                    IsAllSelectChecked = false;
                    using (dataView.DeferRefresh())
                    {
                        dataView.SortDescriptions.Clear();
                        dataView.GroupDescriptions.Clear();
                        dataView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.CategoryName)));
                        dataView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.SymbolName), ListSortDirection.Ascending));
                        dataView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.Description), ListSortDirection.Ascending));
                    }
                }
            }
        }

        #endregion


        #region DataFilter

        private IDictionary<double, Level> allLevels;
        public IDictionary<double, Level> ValidLevels
        {
            get => allLevels;
            set => SetProperty(ref allLevels, value);
        }


        private Level level = null;
        public Level SelectedLevel
        {
            get => level;
            set
            {
                if (SetProperty(ref level, value) && level != null)
                {
                    Properties.Settings.Default.Upgrade();
                    SnoopIntersectionDataByLevel(level);
                }
            }
        }


        private string filterText = string.Empty;
        public string FilterText
        {
            get => filterText;
            set
            {
                if (SetProperty(ref filterText, value))
                {
                    DataViewCollection.Filter = FilterModelCollection;
                    SelectAllVaueHandelCommand();
                    DataViewCollection.Refresh();
                }
            }
        }

        private IList<string> uniqueNames = null;
        public IList<string> UniqueItemNames
        {
            get => uniqueNames;
            set => SetProperty(ref uniqueNames, value);
        }

        private IList<string> GetUniqueStringList(Collection<ElementModel> collection)
        {
            return new SortedSet<string>(collection.Select(c => c.SymbolName).Append(string.Empty)).ToList();
        }

        private bool FilterModelCollection(object obj)
        {
            return string.IsNullOrEmpty(FilterText)
            || obj is not ElementModel model || model.SymbolName.Contains(FilterText)
            || model.SymbolName.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase)
            || model.SymbolName.Equals(FilterText, StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion


        #region SettingsCommand

        public ICommand SettingsCommand { get; private set; }

        [STAThread]
        private void SettingsHandelCommand()
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                if (!IsOptionEnabled)
                {
                    IsOptionEnabled = true;
                }
                else
                {
                    IsDataEnabled = true;
                }
            });
        }

        #endregion


        #region SelectItemCommand
        public ICommand SelectItemCommand { get; private set; }
        private void SelectAllVaueHandelCommand()
        {
            IEnumerable<ElementModel> items = DataViewCollection.OfType<ElementModel>();
            ElementModel firstItem = DataViewCollection.OfType<ElementModel>().FirstOrDefault();
            IsAllSelectChecked = items.All(x => x.IsSelected == firstItem.IsSelected) ? firstItem?.IsSelected : null;
        }

        #endregion


        #region ShowExecuteCommand
        public ICommand ShowExecuteCommand { get; private set; }

        [STAThread]
        private async Task ExecuteHandelCommandAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                UIDocument uidoc = app.ActiveUIDocument;
                Document doc = app.ActiveUIDocument.Document;
                if (documentId.Equals(doc.ProjectInformation.UniqueId))
                {
                    foreach (ElementModel model in DataViewCollection)
                    {
                        if (model.IsSelected && ElementModelData.Remove(model))
                        {
                            Element elem = doc.GetElement(new ElementId(model.IdInt));
                            try
                            {
                                // Set Openning Logic with doc regenerate and transaction RollBack                                   
                                view3d = RevitViewManager.SetCustomSectionBox(uidoc, elem, view3d);
                                RevitViewManager.SetColorElement(uidoc, elem);
                            }
                            finally
                            {
                                Task.Delay(1000).Wait();
                            }

                            break;
                        }
                    }
                    // seletAll update by ViewItems
                    // boolSet to buttom IsDataEnabled
                    UniqueItemNames = GetUniqueStringList(ElementModelData);
                }
            });
        }

        #endregion


        #region CloseCommand

        public ICommand CanselCommand { get; private set; }
        private void CancelCallbackLogic()
        {
            CancellationTokenSource cts = new();
            try
            {
                cts.Cancel(true);
                cancelToken = cts.Token;
            }
            catch (AggregateException)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    _ = Task.Delay(1000).ContinueWith((action) => Logger.Warning("Task cansceled"));
                }
            }
        }

        #endregion


        public void Dispose()
        {
            manager?.Dispose();
            ClearElementDataAsync();
        }
    }
}