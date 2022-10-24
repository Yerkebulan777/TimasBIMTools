using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public class CutVoidDataViewModel : ObservableObject
    {
        public CutVoidDockPaneView DockPanelView { get; set; }
        public static ExternalEvent RevitExternalEvent { get; set; }
        public static CancellationToken cancelToken { get; set; } = CancellationToken.None;
        private static SynchronizationContext context { get; set; } = SynchronizationContext.Current;

        private readonly Mutex mutex = new();
        private readonly string docUniqueId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly APIEventHandler eventHandler = SmartToolApp.ServiceProvider.GetRequiredService<APIEventHandler>();
        private readonly RevitPurginqManager constructManager = SmartToolApp.ServiceProvider.GetRequiredService<RevitPurginqManager>();
        private readonly CutVoidCollisionManager collisionManager = SmartToolApp.ServiceProvider.GetRequiredService<CutVoidCollisionManager>();


        public CutVoidDataViewModel()
        {
            RevitExternalEvent = ExternalEvent.Create(eventHandler);
            //CanselCommand = new RelayCommand(CancelCallbackLogic);
            //SelectItemCommand = new RelayCommand(SelectAllVaueHandelCommand);
            //ShowExecuteCommand = new AsyncRelayCommand(ExecuteHandelCommandAsync);
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
                    ResetCurrentContext();
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
                        SetMEPCategoriesToData();
                        SetCoreMaterialsToData();
                        SetFamilySymbolsToData();
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
                    Properties.Settings.Default.Upgrade();
                    if (category != null && material != null)
                    {
                        GetValidLevelsToData();
                        GetGeneral3DView();
                    }
                }
            }
        }

        #endregion


        #region Temporary

        private Document doc { get; set; }
        private View3D view3d { get; set; }
        private IEnumerable<Element> constructInstances { get; set; }
        private IDictionary<int, ElementId> constructTypeIds { get; set; }

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


        #region FamilySymbol

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

        #endregion


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

        [STAThread]
        public async void StartHandlerExecute()
        {
            DocumentModelCollection = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    constructTypeIds = constructManager.PurgeAndGetValidConstructionTypeIds(doc);
                    return RevitFilterManager.GetDocumentCollection(doc);
                }
                return null;
            });
        }


        [STAThread]
        private void ClearElementDataAsync()
        {
            if (IsStarted)
            {
                IsStarted = false;
                IsDataRefresh = false;
                IsOptionEnabled = false;
                ElementModelData = null;
                EngineerCategories = null;
                StructureMaterials = null;
                FamilySymbols = null;
                ValidLevels = null;
            }
        }


        private void ResetCurrentContext()
        {
            context = CutVoidDockPaneView.UIContext;
            if (SynchronizationContext.Current != context)
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


        private async void GetGeneral3DView()
        {
            view3d ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return docUniqueId.Equals(doc.ProjectInformation.UniqueId) ? RevitViewManager.Get3dView(app.ActiveUIDocument) : null;
            });
        }


        private async void SetMEPCategoriesToData()
        {
            EngineerCategories ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return docUniqueId.Equals(doc.ProjectInformation.UniqueId)
                ? RevitFilterManager.GetEngineerCategories(doc) : null;
            });
        }


        private async void SetCoreMaterialsToData()
        {
            StructureMaterials ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return docUniqueId.Equals(doc.ProjectInformation.UniqueId)
                    ? RevitFilterManager.GetConstructionCoreMaterials(doc, constructTypeIds) : null;
            });
        }


        private async void SetFamilySymbolsToData()
        {
            FamilySymbols ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return docUniqueId.Equals(doc.ProjectInformation.UniqueId)
                    ? RevitFilterManager.GetHostedFamilySymbols(doc, BuiltInCategory.OST_GenericModel) : null;
            });
        }


        private async void GetValidLevelsToData()
        {
            ValidLevels ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return docUniqueId.Equals(doc.ProjectInformation.UniqueId) ? RevitFilterManager.GetValidLevels(doc) : null;
            });
        }


        private async void GetInstancesByCoreMaterialInType(string matName)
        {
            if (!string.IsNullOrEmpty(matName))
            {
                constructInstances = await RevitTask.RunAsync(app =>
                {
                    doc = app.ActiveUIDocument.Document;
                    return docUniqueId.Equals(doc.ProjectInformation.UniqueId)
                        ? RevitFilterManager.GetInstancesByCoreMaterial(doc, constructTypeIds, matName)
                        : new List<Element>();
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
                    bool boolean = (bool)value;
                    if (!DataViewCollection.IsEmpty && value.HasValue)
                    {
                        foreach (ElementModel model in DataViewCollection)
                        {
                            model.IsSelected = boolean;
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
                    // boolSet to buttom IsDataRefresh
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
            ClearElementDataAsync();
            collisionManager?.Dispose();
        }
    }
}