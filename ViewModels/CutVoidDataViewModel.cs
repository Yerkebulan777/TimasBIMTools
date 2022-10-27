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
using Parameter = Autodesk.Revit.DB.Parameter;

namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutVoidDataViewModel : ObservableObject
    {
        private readonly Mutex mutex = new();
        public CutVoidDockPaneView DockPanelView { get; set; }
        public static ExternalEvent RevitExternalEvent { get; set; }
        public static CancellationToken cancelToken { get; set; } = CancellationToken.None;
        private static SynchronizationContext context { get; set; } = SynchronizationContext.Current;

        private readonly string docUniqueId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly TaskScheduler taskContext = TaskScheduler.FromCurrentSynchronizationContext();
        private readonly RevitPurginqManager constructManager = SmartToolApp.ServiceProvider.GetRequiredService<RevitPurginqManager>();
        private readonly CutVoidCollisionManager collisionManager = SmartToolApp.ServiceProvider.GetRequiredService<CutVoidCollisionManager>();


        public CutVoidDataViewModel(APIEventHandler eventHandler)
        {
            RevitExternalEvent = ExternalEvent.Create(eventHandler);
            RefreshDataCommand = new AsyncRelayCommand(RefreshActiveDataHandler);
            //CanselCommand = new RelayCommand(CancelCallbackLogic);
            //SelectItemCommand = new RelayCommand(SelectAllModelHandelCommand);
            //ShowExecuteCommand = new AsyncRelayCommand(ExecuteHandelCommandAsync);
        }


        #region Templory
        private Document doc { get; set; }
        private View3D view3d { get; set; }

        #endregion


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
                        StartHandler();
                        GetGeneral3DView();
                    }
                }
            }
        }


        private bool enabled = false;
        public bool IsOptionEnabled
        {
            get => enabled;
            set
            {
                if (SetProperty(ref enabled, value) && enabled)
                {
                    if (!string.IsNullOrEmpty(docUniqueId))
                    {
                        ResetCurrentContext();
                        GetMEPCategoriesToData();
                        GetCoreMaterialsToData();
                        GetHostedSymbolsToData();
                    }
                }
            }
        }


        private bool refresh = false;
        public bool IsDataRefresh
        {
            get => refresh;
            set
            {
                if (SetProperty(ref refresh, value) && refresh)
                {
                    SnoopIntersectionByInputData();
                    ResetCurrentContext();
                }
            }
        }

        #endregion


        #region Settings

        #region GeneralData

        private DocumentModel document = null;
        public DocumentModel SelectedDocument
        {
            get => document;
            set
            {
                if (SetProperty(ref document, value) && document != null)
                {
                    RefreshActiveData();
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
                    RefreshActiveData();
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
                    RefreshActiveData();
                }
            }
        }


        private ICollection<DocumentModel> documents = null;
        public ICollection<DocumentModel> DocumentCollection
        {
            get => documents;
            set
            {
                if (SetProperty(ref documents, value) && documents != null)
                {
                    Logger.Log("DocumentCollection count:\t" + value.Count.ToString());
                }
            }
        }


        private IDictionary<string, Material> structs = null;
        public IDictionary<string, Material> StructureMaterials
        {
            get => structs;
            set
            {
                if (SetProperty(ref structs, value) && structs != null)
                {
                    Logger.Log("StructureMaterials count:\t" + value.Count.ToString());
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
                    Logger.Log("EngineerCategories count:\t" + value.Count.ToString());
                }
            }
        }

        #endregion


        #region FamilySymbol

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


        private FamilySymbol wallHole = null;
        public FamilySymbol WallOpenning
        {
            get => wallHole;
            set
            {
                if (SetProperty(ref wallHole, value) && wallHole != null)
                {
                    ActivateFamilySimbol(wallHole);
                    GetSymbolSharedParameters(wallHole);
                    Properties.Settings.Default.RectangSymbolUniqueId = wallHole.UniqueId;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private FamilySymbol floorHole = null;
        public FamilySymbol FloorOpenning
        {
            get => floorHole;
            set
            {
                if (SetProperty(ref floorHole, value) && floorHole != null)
                {
                    ActivateFamilySimbol(floorHole);
                    GetSymbolSharedParameters(floorHole);
                    Properties.Settings.Default.RoundedSymbolUniqueId = floorHole.UniqueId;
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
                    RefreshActiveData();
                }
            }
        }

        #endregion


        #region SizeData

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

        [STAThread]
        public async void StartHandler()
        {
            DocumentCollection = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    collisionManager.InitializeElementTypeIdData(doc);
                    DockPanelView.ActiveDocTitle.Content = doc.Title.ToUpper();
                    return RevitFilterManager.GetDocumentCollection(doc);
                }
                return null;
            });
        }


        [STAThread]
        private void ResetCurrentContext()
        {
            context = DataViewCollection?.SourceCollection as SynchronizationContext;
            if (context != null && SynchronizationContext.Current != context)
            {
                try
                {
                    SynchronizationContext.SetSynchronizationContext(context);
                    Logger.ThreadProcessLog("Complited: " + nameof(ResetCurrentContext));
                }
                catch (Exception ex)
                {
                    Logger.Error(nameof(ResetCurrentContext) + ex.Message);
                }
            }
        }


        [STAThread]
        private void ClearElementDataAsync()
        {
            if (IsStarted)
            {
                IsStarted = false;
                IsDataRefresh = false;
                IsOptionEnabled = false;
                FamilySymbols = null;
                ElementModelData = null;
                EngineerCategories = null;
                StructureMaterials = null;
            }
        }


        [STAThread]
        private async void GetGeneral3DView()
        {
            view3d ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return RevitViewManager.Get3dView(app.ActiveUIDocument);
            });
        }


        [STAThread]
        private async void GetMEPCategoriesToData()
        {
            EngineerCategories ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return RevitFilterManager.GetEngineerCategories(doc);
            });
        }


        [STAThread]
        private async void GetCoreMaterialsToData()
        {
            StructureMaterials ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return collisionManager.GetStructureCoreMaterialData(doc);
            });
        }


        [STAThread]
        private async void GetHostedSymbolsToData()
        {
            FamilySymbols ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return RevitFilterManager.GetHostedFamilySymbols(doc, BuiltInCategory.OST_GenericModel);
            });
        }


        [STAThread]
        private async void SnoopIntersectionByInputData()
        {
            if (document != null && material != null && category != null)
            {
                ElementModelData = await RevitTask.RunAsync(app =>
                {
                    doc = app.ActiveUIDocument.Document;
                    Properties.Settings.Default.Upgrade();
                    return collisionManager.GetCollisionByInputData(doc, document, material, category).ToObservableCollection();
                });
            }
        }


        [STAThread]
        private async void ActivateFamilySimbol(FamilySymbol symbol)
        {
            await RevitTask.RunAsync(app =>
            {
                if (symbol.IsValidObject && !symbol.IsActive)
                {
                    symbol.Activate();
                }
            });
        }


        [STAThread]
        private async void GetSymbolSharedParameters(FamilySymbol symbol)
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


        [STAThread]
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


        private async void RefreshActiveData()
        {
            if (IsDataRefresh)
            {
                IsDataRefresh = false;
                await RefreshActiveDataHandler();
            }
        }


        public ICommand RefreshDataCommand { get; private set; }
        private async Task RefreshActiveDataHandler()
        {
            IsDataRefresh = false;
            await Task.Delay(100).ContinueWith(_ =>
            {
                if (document != null && material != null && category != null)
                {
                    IsDataRefresh = true;
                }
            }, taskContext);
        }

        #endregion


        #region DataGrid

        private bool? isSelected = false;
        public bool? IsAllSelectChecked
        {
            get => isSelected;
            set
            {
                if (SetProperty(ref isSelected, value) && dataView != null)
                {
                    if (!DataViewCollection.IsEmpty && value.HasValue)
                    {
                        bool booleanValue = value.Value;
                        foreach (ElementModel model in DataViewCollection)
                        {
                            model.IsSelected = booleanValue;
                        }
                    }

                }
            }
        }


        private ObservableCollection<ElementModel> collection = null;
        public ObservableCollection<ElementModel> ElementModelData
        {
            get => collection;
            set
            {
                if (SetProperty(ref collection, value) && collection != null)
                {
                    DataViewCollection = CollectionViewSource.GetDefaultView(collection) as ListCollectionView;
                    GetUniqueSymbolNameList(collection);
                    GetUniqueLevelNameList(collection);
                    DataViewCollection.Refresh();
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
                            dataView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.FamilyName)));
                            dataView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.LevelName), ListSortDirection.Ascending));
                            dataView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.SymbolName), ListSortDirection.Ascending));
                        }
                    }
                }
            }
        }

        #endregion


        #region DataFilter

        private string levelText;
        public string LevelTextFilter
        {
            get => levelText;
            set
            {
                if (SetProperty(ref levelText, value))
                {
                    DataViewCollection.Filter = FilterModelCollection;
                    SelectAllModelHandelCommand();
                    DataViewCollection.Refresh();
                }
            }
        }


        private string symbolText;
        public string SymbolTextFilter
        {
            get => symbolText;
            set
            {
                if (SetProperty(ref symbolText, value))
                {
                    DataViewCollection.Filter = FilterModelCollection;
                    SelectAllModelHandelCommand();
                    DataViewCollection.Refresh();
                }
            }
        }


        private IList<string> levelNames;
        public IList<string> UniqueLevelNames
        {
            get => levelNames;
            set => SetProperty(ref levelNames, value);
        }


        private IList<string> uniqueNames = null;
        public IList<string> UniqueSymbolNames
        {
            get => uniqueNames;
            set => SetProperty(ref uniqueNames, value);
        }


        private bool FilterModelCollection(object obj)
        {
            return (string.IsNullOrEmpty(LevelTextFilter) && string.IsNullOrEmpty(SymbolTextFilter))
            || obj is not ElementModel model || (model.LevelName.Contains(LevelTextFilter) && model.SymbolName.Contains(SymbolTextFilter))
            || (model.LevelName.Equals(LevelTextFilter, StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(model.SymbolName))
            || (model.SymbolName.Equals(SymbolTextFilter, StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(model.LevelName));
        }


        private void GetUniqueLevelNameList(IList<ElementModel> collection)
        {
            UniqueLevelNames = new SortedSet<string>(collection.Select(c => c.LevelName).Append(string.Empty)).ToList();
        }

        private void GetUniqueSymbolNameList(IList<ElementModel> collection)
        {
            UniqueSymbolNames = new SortedSet<string>(collection.Select(c => c.SymbolName).Append(string.Empty)).ToList();
        }

        #endregion


        #region SelectItemCommand
        public ICommand SelectItemCommand { get; private set; }
        private void SelectAllModelHandelCommand()
        {
            IEnumerable<ElementModel> items = DataViewCollection.OfType<ElementModel>();
            ElementModel firstItem = DataViewCollection.OfType<ElementModel>().FirstOrDefault();
            IsAllSelectChecked = items.All(x => x.IsSelected == firstItem.IsSelected) ? firstItem?.IsSelected : false;
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
                    // boolSet to buttom IsDataRefresh
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
            ClearElementDataAsync();
            collisionManager?.Dispose();
        }
    }
}