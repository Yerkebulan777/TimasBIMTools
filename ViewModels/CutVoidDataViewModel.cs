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
    public class CutVoidDataViewModel : ObservableObject, IDisposable
    {
        public CutVoidDockPaneView DockPanelView { get; set; } = null;
        public SynchronizationContext SyncContext { get; set; } = SynchronizationContext.Current;
        public TaskScheduler TaskContext { get; set; } = TaskScheduler.FromCurrentSynchronizationContext();

        private Document doc { get; set; } = null;
        private View3D view3d { get; set; } = null;
        private IEnumerable<Element> constructionInstances { get; set; } = null;
        private IDictionary<int, ElementId> constructionTypeIds { get; set; } = null;
        private CancellationToken cancelToken { get; set; } = CancellationToken.None;

        private static readonly IServiceProvider provider = SmartToolApp.ServiceProvider;
        private readonly string documentId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly CutVoidCollisionManager collisionManager = provider.GetRequiredService<CutVoidCollisionManager>();
        private readonly RevitPurginqManager constructManager = provider.GetRequiredService<RevitPurginqManager>();


        public CutVoidDataViewModel()
        {
            CanselCommand = new RelayCommand(CancelCallbackLogic);
            SettingsCommand = new RelayCommand(SettingsHandelCommand);
            SelectItemCommand = new RelayCommand(SelectAllVaueHandelCommand);
            ShowExecuteCommand = new AsyncRelayCommand(ExecuteHandelCommandAsync);
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
                if (SetProperty(ref enableOpt, value))
                {
                    ClearElementDataAsync();
                    SetMEPCategoriesToData();
                    SetCoreMaterialsToData();
                    SetFamilySymbolsToData();
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


        private IDictionary<string, Category> categories = null;
        public IDictionary<string, Category> EngineerCategories
        {
            get => categories;
            set
            {
                if (value != null)
                {
                    if (SetProperty(ref categories, value))
                    {
                        Logger.Log(value.Count.ToString());
                    }
                }
            }
        }


        private IDictionary<string, Material> structMats = null;
        public IDictionary<string, Material> StructureMaterials
        {
            get => structMats;
            set
            {
                if (value != null)
                {
                    _ = SetProperty(ref structMats, value);
                    if (SetProperty(ref structMats, value))
                    {
                        Logger.Log(value.Count.ToString());
                    }
                }
            }
        }


        private IDictionary<string, FamilySymbol> symbols = null;
        public IDictionary<string, FamilySymbol> FamilySymbols
        {
            get => symbols;
            set
            {
                if (value != null)
                {
                    if (DockPanelView.Dispatcher.Invoke(() => SetProperty(ref symbols, value)))
                    {
                        Logger.Log(value.Count.ToString());
                    }
                }
            }
        }


        private DocumentModel docModel = null;
        public DocumentModel SelectedDocModel
        {
            get => docModel;
            set
            {
                if (value != null)
                {
                    DockPanelView.Dispatcher.Invoke(delegate
                    {
                        if (SetProperty(ref docModel, value))
                        {
                            collisionManager.SearchDoc = docModel.Document;
                            collisionManager.SearchGlobal = docModel.Transform;
                            collisionManager.SearchInstance = docModel.LinkInstance;
                        }
                    });
                }
            }
        }


        private Category category = null;
        public Category SelectedCategory
        {
            get => category;
            set
            {
                if (value != null)
                {
                    DockPanelView.Dispatcher.Invoke(delegate
                    {
                        if (SetProperty(ref category, value))
                        {
                            collisionManager.SearchCatId = category.Id;
                        }
                    });
                }
            }
        }


        private Material material = null;
        public Material SelectedMaterial
        {
            get => material;
            set
            {
                if (value != null)
                {
                    if (DockPanelView.Dispatcher.Invoke(() => SetProperty(ref material, value)))
                    {
                        GetInstancesByCoreMaterialInType(material.Name);
                    }
                }
            }
        }


        private FamilySymbol rectang = null;
        public FamilySymbol RectangSymbol
        {
            get => rectang;
            set
            {
                if (value != null)
                {
                    if (DockPanelView.Dispatcher.Invoke(() => SetProperty(ref rectang, value)))
                    {
                        Properties.Settings.Default.RectangSymbol = rectang.UniqueId;
                        Properties.Settings.Default.Save();
                        ActivateFamilySimbolAsync(rectang);
                    }
                }
            }
        }


        private FamilySymbol rounded = null;
        public FamilySymbol RoundedSymbol
        {
            get => rounded;
            set
            {
                if (value != null)
                {
                    if (DockPanelView.Dispatcher.Invoke(() => SetProperty(ref rounded, value)))
                    {
                        Properties.Settings.Default.RoundedSymbol = rounded.UniqueId;
                        Properties.Settings.Default.Save();
                        ActivateFamilySimbolAsync(rounded);
                    }
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
                }
            }
        }

        #endregion


        #region Methods

        public void StartExecuteHandler()
        {
            RevitTask.RunAsync(async app =>
            {
                IsStarted = true;
                IsDataEnabled = false;
                IsOptionEnabled = false;
                doc = app.ActiveUIDocument.Document;
                SyncContext = SynchronizationContext.Current;
                TaskContext = TaskScheduler.FromCurrentSynchronizationContext();
                Properties.Settings.Default.ActiveDocumentUniqueId = doc.ProjectInformation.UniqueId;
                DocumentModelCollection = RevitDocumentManager.GetDocumentCollection(doc).ToObservableCollection();
                constructionTypeIds = constructManager.PurgeAndGetValidConstructionTypeIds(doc);
                CommandManager.InvalidateRequerySuggested();
                Properties.Settings.Default.Reload();
                Properties.Settings.Default.Save();
                await Task.Yield();
            }).Dispose();
        }


        private async void ClearElementDataAsync()
        {
            if (IsOptionEnabled || IsDataEnabled)
            {
                await Task.Delay(1000).ContinueWith(_ =>
                {
                    ElementModelData = new ObservableCollection<ElementModel>();
                    EngineerCategories = new Dictionary<string, Category>();
                    StructureMaterials = new Dictionary<string, Material>();
                    FamilySymbols = new Dictionary<string, FamilySymbol>();
                    Properties.Settings.Default.Reset();
                }, TaskContext);
            }
        }


        private void GetGeneral3DViewAsync()
        {
            RevitTask.RunAsync(app =>
            {
                view3d = RevitViewManager.Get3dView(app.ActiveUIDocument);
            }).Dispose();
        }


        private void SetMEPCategoriesToData()
        {
            RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                EngineerCategories = RevitFilterManager.GetEngineerCategories(doc);
            }).Dispose();
        }


        private void SetCoreMaterialsToData()
        {
            RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                StructureMaterials = RevitFilterManager.GetConstructionCoreMaterials(doc, constructionTypeIds);
            }).Dispose();
        }


        private void SetFamilySymbolsToData()
        {
            RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                FamilySymbols = RevitFilterManager.GetHostedFamilySymbols(doc, BuiltInCategory.OST_GenericModel);
            }).Dispose();
        }


        private void SetValidLevelsToData()
        {
            RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                ValidLevels = RevitFilterManager.GetValidLevels(doc);
            }).Dispose();
        }


        private void GetInstancesByCoreMaterialInType(string matName)
        {
            RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                constructionInstances = RevitFilterManager.GetInstancesByCoreMaterial(doc, constructionTypeIds, matName);
            }).Dispose();
        }


        private void SnoopIntersectionDataByLevel(Level level)
        {
            RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                ElementModelData = collisionManager.GetCollisionByLevel(doc, level, constructionInstances).ToObservableCollection();
            }).Dispose();
        }


        private void ActivateFamilySimbolAsync(FamilySymbol symbol)
        {
            RevitTask.RunAsync(app =>
           {
               if (symbol != null && !symbol.IsActive)
               {
                   symbol.Activate();
               }
           }).Dispose();
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
                    if (dataModels != null && dataModels.Count > 0)
                    {
                        DataViewCollection = CollectionViewSource.GetDefaultView(dataModels) as ListCollectionView;
                        UniqueItemNames = GetUniqueStringList(dataModels);
                        DataViewCollection.Refresh();
                    }
                }
            }
        }


        private ListCollectionView dataView = null;
        public ListCollectionView DataViewCollection
        {
            get => dataView;
            set
            {
                IsAllSelectChecked = false;
                if (SetProperty(ref dataView, value))
                {
                    if (dataView != null && !dataView.IsEmpty)
                    {
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
            collisionManager?.Dispose();
            ClearElementDataAsync();
        }

    }
}