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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Document = Autodesk.Revit.DB.Document;
using Parameter = Autodesk.Revit.DB.Parameter;

namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutVoidDataViewModel : ObservableObject
    {
        public CutVoidDockPaneView DockPanelView { get; set; }
        private static SynchronizationContext context { get; set; }
        public static ExternalEvent RevitExternalEvent { get; set; }
        public static CancellationToken cancelToken { get; set; } = CancellationToken.None;

        private static readonly AutoResetEvent manualResetEvent = new(true);
        private readonly string docUniqueId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly TaskScheduler taskContext = TaskScheduler.FromCurrentSynchronizationContext();
        private readonly RevitPurginqManager constructManager = SmartToolApp.ServiceProvider.GetRequiredService<RevitPurginqManager>();
        private readonly CutVoidCollisionManager collisionManager = SmartToolApp.ServiceProvider.GetRequiredService<CutVoidCollisionManager>();


        public CutVoidDataViewModel(APIEventHandler eventHandler)
        {
            RevitExternalEvent = ExternalEvent.Create(eventHandler);
            RefreshDataCommand = new AsyncRelayCommand(RefreshActiveDataHandler);
            ShowExecuteCommand = new AsyncRelayCommand(ExecuteHandelCommandAsync);
            //CanselCommand = new RelayCommand(CancelCallbackLogic);
        }



        #region Templory
        private Document doc { get; set; }
        private View3D view3d { get; set; }
        private ElementId patternId { get; set; }

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
                        ResetCurrentContext();
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
                if (SetProperty(ref enabled, value) && started && enabled)
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
                }
            }
        }

        #endregion


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


        private FamilySymbol wallOpenning = null;
        public FamilySymbol WallOpenning
        {
            get => wallOpenning;
            set
            {
                if (SetProperty(ref wallOpenning, value) && wallOpenning != null)
                {
                    ActivateFamilySimbol(wallOpenning);
                    GetSymbolSharedParameters(wallOpenning);
                    Properties.Settings.Default.RectangSymbolUniqueId = wallOpenning.UniqueId;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private FamilySymbol floorOpenning = null;
        public FamilySymbol FloorOpenning
        {
            get => floorOpenning;
            set
            {
                if (SetProperty(ref floorOpenning, value) && floorOpenning != null)
                {
                    ActivateFamilySimbol(floorOpenning);
                    GetSymbolSharedParameters(floorOpenning);
                    Properties.Settings.Default.RoundedSymbolUniqueId = floorOpenning.UniqueId;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private IList<Definition> definitions;
        public IList<Definition> ParameterDefinitions
        {
            get => definitions;
            set => SetProperty(ref definitions, value);
        }


        private Definition definition;
        public Definition SelectedDefinition
        {
            get => definition;
            set => SetProperty(ref definition, value);
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
        private void ClearAndResetData()
        {
            if (IsStarted)
            {
                IsStarted = false;
                IsDataRefresh = false;
                IsOptionEnabled = false;
                DocumentCollection = null;
                EngineerCategories = null;
                StructureMaterials = null;
                ElementModelData = null;
                FamilySymbols = null;
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
                    using Transaction tx = new(app.ActiveUIDocument.Document);
                    TransactionStatus status = tx.Start("Activate family");
                    symbol.Activate();
                    status = tx.Commit();
                }
            });
        }


        [STAThread]
        private async void GetSymbolSharedParameters(FamilySymbol symbol)
        {
            ParameterDefinitions = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                List<Definition> definitions = new(3);
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    foreach (Parameter param in symbol.GetOrderedParameters())
                    {
                        if (!param.IsReadOnly && param.UserModifiable)
                        {
                            Definition defin = param.Definition;
                            switch (param.StorageType)
                            {
                                case StorageType.Double:
                                    definitions.Add(defin);
                                    break;
                                case StorageType.String:
                                    definitions.Add(defin);
                                    break;
                                default: break;
                            }
                        }
                    }
                }
                return definitions;
            });
        }


        internal async void GetElementInViewByIntId(ElementId id)
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    if (manualResetEvent.WaitOne())
                    {
                        Element elem = doc.GetElement(id);
                        System.Windows.Clipboard.SetText(id.ToString());
                        RevitViewManager.ShowElement(app.ActiveUIDocument, elem);
                        _ = manualResetEvent.Set();
                    }
                }
            });
        }


        private async void RefreshActiveData()
        {
            if (IsDataRefresh)
            {
                await RefreshActiveDataHandler();
            }
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
                }
            }
        }


        private ListCollectionView dataView = null;
        public ListCollectionView DataViewCollection
        {
            get => dataView;
            set
            {
                if (SetProperty(ref dataView, value))
                {
                    VerifyAllSelectedData();
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
                    DataViewCollection.Refresh();
                    VerifyAllSelectedData();
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
                    DataViewCollection.Refresh();
                    VerifyAllSelectedData();
                }
            }
        }


        private IList<string> levelNames;
        public IList<string> UniqueLevelNames
        {
            get => levelNames;
            set => SetProperty(ref levelNames, value);
        }


        private IList<string> symbolNames = null;
        public IList<string> UniqueSymbolNames
        {
            get => symbolNames;
            set => SetProperty(ref symbolNames, value);
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


        #region VerifyAllSelectedData
        private void VerifyAllSelectedData()
        {
            if (manualResetEvent.WaitOne())
            {
                if (DataViewCollection.IsInUse)
                {
                    DataViewCollection.Refresh();
                    IEnumerable<ElementModel> items = DataViewCollection.OfType<ElementModel>();
                    ElementModel firstItem = DataViewCollection.OfType<ElementModel>().FirstOrDefault();
                    IsAllSelectChecked = items.All(x => x.IsSelected == firstItem.IsSelected) ? firstItem.IsSelected : null;
                }
                else
                {
                    IsAllSelectChecked = false;
                }
                _ = manualResetEvent.Set();
            }
        }
        #endregion


        #region RefreshDataCommand
        public ICommand RefreshDataCommand { get; private set; }
        private async Task RefreshActiveDataHandler()
        {
            IsDataRefresh = false;
            Show3DViewAsync(view3d);
            if (document != null && material != null && category != null)
            {
                await Task.Delay(1000).ContinueWith(_ =>
                {
                    IsDataRefresh = true;
                    IsOptionEnabled = false;
                }, taskContext);
            }
        }


        private async void Show3DViewAsync(View3D view3d)
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    if (manualResetEvent.WaitOne())
                    {
                        patternId = RevitViewManager.GetSolidFillPatternId(doc);
                        RevitViewManager.Show3DView(app.ActiveUIDocument, view3d);
                        _ = manualResetEvent.Set();
                    }
                }
            });
        }

        #endregion


        #region ShowExecuteCommand
        public ICommand ShowExecuteCommand { get; private set; }

        [STAThread]
        private async Task ExecuteHandelCommandAsync()
        {
            if (DataViewCollection?.IsEmpty == false)
            {
                object item = DataViewCollection.GetItemAt(0);
                DataGrid dataGrid = DockPanelView.dataGridView;
                IsOptionEnabled = await RevitTask.RunAsync(app =>
                {
                    if (item is ElementModel model && model.IsSelected)
                    {
                        dataGrid.SelectedItem = item;
                        dataGrid.ScrollIntoView(item);
                        doc = app.ActiveUIDocument.Document;
                        if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                        {
                            bool viewBool = SetSectionBoxModelView(app.ActiveUIDocument, model, view3d, patternId);
                            bool voidBool = collisionManager.CreateOpening(doc, model, wallOpenning, floorOpenning);
                            if (viewBool && voidBool && ElementModelData.Remove(model))
                            {

                                Logger.Log("Remove item:\t" + item.ToString());
                            }

                        }
                    }
                    return DataViewCollection.IsEmpty;
                });
            }
        }


        public bool SetSectionBoxModelView(UIDocument uidoc, ElementModel model, View3D view3d, ElementId patternId)
        {
            RevitViewManager.SetCustomColorInView(uidoc, view3d, patternId, model.Instanse);
            return RevitViewManager.SetCustomSectionBox(uidoc, model.Origin, view3d);
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
            ClearAndResetData();
            collisionManager?.Dispose();
        }
    }
}