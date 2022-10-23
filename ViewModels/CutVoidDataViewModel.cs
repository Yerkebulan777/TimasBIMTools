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
using Document = Autodesk.Revit.DB.Document;


namespace RevitTimasBIMTools.ViewModels
{
    public class CutVoidDataViewModel : ObservableObject
    {
        public TaskScheduler TaskContext { get; set; }
        public ExternalEvent externalEvent { get; set; }
        public CutVoidDockPaneView DockPanelView { get; set; }
        public SynchronizationContext SyncContext { get; set; }
        public CancellationToken cancelToken { get; set; } = CancellationToken.None;


        private readonly Mutex mutex = new();
        private readonly string docUniqueId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly APIEventHandler eventHandler = SmartToolApp.ServiceProvider.GetRequiredService<APIEventHandler>();
        private readonly RevitPurginqManager constructManager = SmartToolApp.ServiceProvider.GetRequiredService<RevitPurginqManager>();
        private readonly CutVoidCollisionManager collisionManager = SmartToolApp.ServiceProvider.GetRequiredService<CutVoidCollisionManager>();


        public CutVoidDataViewModel()
        {
            externalEvent = ExternalEvent.Create(eventHandler);
            CanselCommand = new RelayCommand(CancelCallbackLogic);
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
                    if (started)
                    {
                        StartHandlerExecute();
                    }
                    else
                    {
                        ClearElementDataAsync();
                    }
                }
            }
        }


        private bool enableOpt = false;
        public bool IsOptionEnabled
        {
            get => enableOpt;
            set
            {
                if (SetProperty(ref enableOpt, value) && enableOpt)
                {
                    Properties.Settings.Default.Upgrade();
                    if (!string.IsNullOrEmpty(docUniqueId))
                    {
                        SetMEPCategoriesToData();
                        SetCoreMaterialsToDataAsync();
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
                if (SetProperty(ref enableData, value) && enableData)
                {
                    Properties.Settings.Default.Upgrade();
                    if (!string.IsNullOrEmpty(docUniqueId))
                    {
                        GetValidLevelsToData();
                        GetGeneral3DView();
                    }
                }
            }
        }

        #endregion


        #region Temporary

        private Document doc { get; set; } = null;
        private View3D view3d { get; set; } = null;
        private IDictionary<int, ElementId> constructTypeIds { get; set; } = null;
        private IEnumerable<Element> constructInstances { get; set; } = null;

        #endregion


        #region Settings

        private DocumentModel docModel = null;
        public DocumentModel SelectedDocModel
        {
            get => docModel;
            set
            {
                if (SetProperty(ref docModel, value) && docModel != null)
                {
                    collisionManager.SearchDoc = docModel.Document;
                    collisionManager.SearchGlobal = docModel.Transform;
                    collisionManager.SearchInstance = docModel.LinkInstance;
                }
            }
        }


        private ICollection<DocumentModel> documents = null;
        public ICollection<DocumentModel> DocumentModelCollection
        {
            get => documents;
            set
            {
                if (SetProperty(ref documents, value) && documents != null)
                {
                    Logger.Log("\tcount:\t" + value.Count.ToString());
                    SelectedDocModel = documents.FirstOrDefault();
                }
            }
        }


        private IDictionary<string, Category> categos = null;
        public IDictionary<string, Category> EngineerCategories
        {
            get => categos;
            set
            {
                if (SetProperty(ref categos, value) && categos != null)
                {
                    Logger.Log("\tcount:\t" + value.Count.ToString());
                }
            }
        }


        private IDictionary<string, Material> structs = null;
        public IDictionary<string, Material> StructureMaterials
        {
            get => structs;
            set
            {
                if (value != null)
                {
                    if (SetProperty(ref structs, value) && structs != null)
                    {
                        Logger.Log("\tcount:\t" + value.Count.ToString());
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
                if (SetProperty(ref symbols, value) && symbols != null)
                {
                    Logger.Log("\tcount:\t" + value.Count.ToString());
                }
            }
        }


        private Category category = null;
        public Category SelectedCategory
        {
            get => category;
            set
            {
                if (SetProperty(ref category, value) && category != null)
                {
                    collisionManager.SearchCatId = category.Id;
                }
            }
        }


        private Material material = null;
        public Material SelectedMaterial
        {
            get => material;
            set
            {
                if (SetProperty(ref material, value) && material != null)
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
                if (SetProperty(ref rectang, value) && rectang != null)
                {
                    ActivateFamilySimbolAsync(rectang);
                    GetSymbolSharedParametersAsync(rectang);
                    Properties.Settings.Default.RectangSymbolUniqueId = rectang.UniqueId;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private FamilySymbol rounded = null;
        public FamilySymbol RoundedSymbol
        {
            get => rounded;
            set
            {
                if (SetProperty(ref rounded, value) && rounded != null)
                {
                    ActivateFamilySimbolAsync(rounded);
                    GetSymbolSharedParametersAsync(rounded);
                    Properties.Settings.Default.RoundedSymbolUniqueId = rounded.UniqueId;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private IList<Parameter> shared = null;
        public IList<Parameter> SharedParameters
        {
            get => shared;
            set
            {
                if (SetProperty(ref shared, value) && shared != null)
                {

                }
            }
        }


        #region Sizes

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


        #endregion


        #region Methods

        public async void StartHandlerExecute()
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                SyncContext = SynchronizationContext.Current;
                TaskContext = TaskScheduler.FromCurrentSynchronizationContext();
                if (ExternalEventRequest.Accepted == externalEvent.Raise())
                {
                    constructTypeIds = constructManager.PurgeAndGetValidConstructionTypeIds(doc);
                    DocumentModelCollection = RevitFilterManager.GetDocumentCollection(doc);
                }
            });
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
                    ValidLevels = new Dictionary<double, Level>();
                }, TaskContext);
            }
        }


        private async void GetGeneral3DView()
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    view3d = RevitViewManager.Get3dView(app.ActiveUIDocument);
                }
            });
        }


        private async void SetMEPCategoriesToData()
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    EngineerCategories ??= RevitFilterManager.GetEngineerCategories(doc);
                }
            });
        }


        private async void SetCoreMaterialsToDataAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    StructureMaterials ??= RevitFilterManager.GetConstructionCoreMaterials(doc, constructTypeIds);
                }
            });
        }


        private async void SetFamilySymbolsToData()
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    FamilySymbols ??= RevitFilterManager.GetHostedFamilySymbols(doc, BuiltInCategory.OST_GenericModel);
                }
            });
        }


        private async void GetValidLevelsToData()
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    ValidLevels ??= RevitFilterManager.GetValidLevels(doc);
                }
            });
        }


        private async void GetInstancesByCoreMaterialInType(string matName)
        {
            if (!string.IsNullOrEmpty(matName))
            {
                await RevitTask.RunAsync(app =>
                {
                    doc = app.ActiveUIDocument.Document;
                    if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                    {
                        constructInstances = RevitFilterManager.GetInstancesByCoreMaterial(doc, constructTypeIds, matName);
                    }
                });
            }
        }


        private async void SnoopIntersectionDataByLevel(Level level)
        {
            if (level != null && level.IsValidObject)
            {
                await RevitTask.RunAsync(app =>
                {
                    doc = app.ActiveUIDocument.Document;
                    if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                    {
                        ElementModelData = collisionManager.GetCollisionByLevel(doc, level, constructInstances).ToObservableCollection();
                    }
                });
            }
        }


        private async void ActivateFamilySimbolAsync(FamilySymbol symbol)
        {
            await RevitTask.RunAsync(app =>
            {
                if (symbol.IsValidObject && !symbol.IsActive)
                {
                    symbol.Activate();
                }
            });
        }


        private async void GetSymbolSharedParametersAsync(FamilySymbol symbol)
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                SharedParameters = new List<Parameter>(5);
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    foreach (Parameter param in symbol.GetOrderedParameters())
                    {
                        if (!param.IsReadOnly)
                        {
                            switch (param.StorageType)
                            {
                                case StorageType.Double:
                                    SharedParameters.Add(param);
                                    break;
                                case StorageType.String:
                                    SharedParameters.Add(param);
                                    break;
                                default: break;
                            }
                        }
                    }
                }
            });
        }


        internal async void GetElementInViewByIntId(ElementId id)
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    if (mutex.WaitOne())
                    {
                        Element elem = doc.GetElement(id);
                        System.Windows.Clipboard.SetText(id.ToString());
                        RevitViewManager.ShowElement(app.ActiveUIDocument, elem);
                        mutex.ReleaseMutex();
                    }
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
                            dataView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.Description)));
                            dataView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.SymbolName), ListSortDirection.Ascending));
                            dataView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.FamilyName), ListSortDirection.Ascending));
                        }
                    }
                }
            }
        }

        #endregion


        #region DataFilter

        private IDictionary<double, Level> levels;
        public IDictionary<double, Level> ValidLevels
        {
            get => levels;
            set
            {
                if (SetProperty(ref levels, value) && levels != null)
                {
                    DockPanelView.ComboLevelFilter.SelectedIndex = 0;
                }
            }
        }


        private Level level = null;
        public Level SelectedLevel
        {
            get => level;
            set
            {
                if (SetProperty(ref level, value) && level != null)
                {
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
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    foreach (ElementModel model in DataViewCollection)
                    {
                        if (model.IsSelected && ElementModelData.Remove(model))
                        {
                            Element elem = doc.GetElement(model.Instanse.Id);
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