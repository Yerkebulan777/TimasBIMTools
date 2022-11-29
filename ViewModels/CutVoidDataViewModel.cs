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
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Document = Autodesk.Revit.DB.Document;
using Parameter = Autodesk.Revit.DB.Parameter;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutVoidDataViewModel : ObservableObject
    {
        public CutVoidDockPaneView DockPanelView { get; set; }
        public static ExternalEvent RevitExternalEvent { get; set; }

        private readonly string docUniqueId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly TaskScheduler taskContext = TaskScheduler.FromCurrentSynchronizationContext();
        private readonly RevitPurginqManager constructManager = SmartToolApp.ServiceProvider.GetRequiredService<RevitPurginqManager>();
        private readonly CutVoidCollisionManager collisionManager = SmartToolApp.ServiceProvider.GetRequiredService<CutVoidCollisionManager>();


        public CutVoidDataViewModel(APIEventHandler eventHandler)
        {
            RevitExternalEvent = ExternalEvent.Create(eventHandler);
            RefreshDataCommand = new RelayCommand(RefreshActiveDataHandler);
            ShowCollisionCommand = new AsyncRelayCommand(ShowHandelCommandAsync);
            OkCanselCommand = new AsyncRelayCommand(OkCanselHandelCommandAsync);
        }


        #region Templory

        private Document doc = null;
        private object currentItem = null;

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
                if (SetProperty(ref enabled, value) && started && enabled)
                {
                    if (!string.IsNullOrEmpty(docUniqueId))
                    {
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
                    RefreshActiveDataHandler();
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
                    RefreshActiveDataHandler();
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
                    RefreshActiveDataHandler();
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


        private int minDepth = Properties.Settings.Default.MinDepthSizeInMm;
        public int MinDepthSize
        {
            get => minDepth;
            set
            {
                if (SetProperty(ref minDepth, value))
                {
                    Properties.Settings.Default.MinDepthSizeInMm = minDepth;
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
                    Properties.Settings.Default.CutOffsetInMm = cutOffset;
                    Properties.Settings.Default.Save();
                }
            }
        }

        #endregion


        #region Methods

        public async void StartHandler()
        {
            DocumentCollection = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    DockPanelView.ActiveDocTitle.Content = doc.Title;
                    collisionManager.InitializeElementTypeIdData(doc);
                    return RevitFilterManager.GetDocumentCollection(doc);
                }
                return null;
            });
        }


        private async void ClearAndResetData()
        {
            if (IsStarted)
            {
                await Task.Delay(1000)
                .ContinueWith(_ =>
                {
                    IsStarted = false;
                    IsDataRefresh = false;
                    IsOptionEnabled = false;
                    DocumentCollection = null;
                    EngineerCategories = null;
                    StructureMaterials = null;
                    ElementModelData = null;
                    SymbolTextFilter = null;
                    LevelTextFilter = null;
                    FamilySymbols = null;
                    currentItem = null;
                    view3d = null;

                }, taskContext);
            }
        }


        private async void GetGeneral3DView()
        {
            view3d ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return RevitViewManager.Get3dView(app.ActiveUIDocument);
            });
        }


        private async void GetMEPCategoriesToData()
        {
            EngineerCategories ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return RevitFilterManager.GetEngineerCategories(doc);
            });
        }


        private async void GetCoreMaterialsToData()
        {
            StructureMaterials ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return collisionManager.GetStructureCoreMaterialData(doc);
            });
        }


        private async void GetHostedSymbolsToData()
        {
            FamilySymbols ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return RevitFilterManager.GetHostedFamilySymbols(doc, BuiltInCategory.OST_GenericModel);
            });
        }


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


        internal void ShowElementModelView(ElementModel model)
        {
            if (model != null && model.Instanse.IsValidObject)
            {
                Task task = RevitTask.RunAsync(app =>
                {
                    doc = app.ActiveUIDocument.Document;
                    if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                    {
                        UIDocument uidoc = app.ActiveUIDocument;
                        System.Windows.Clipboard.SetText(model.Instanse.Id.ToString());
                        uidoc.Selection.SetElementIds(new List<ElementId> { model.Instanse.Id });
                        RevitViewManager.ShowModelInPlanView(uidoc, model, ViewDiscipline.Mechanical);
                    }
                });
            }
        }

        #endregion


        #region DataGrid

        private bool? allSelected = false;
        public bool? AllSelectChecked
        {
            get => allSelected;
            set
            {
                if (SetProperty(ref allSelected, value))
                {
                    if (viewData != null && value.HasValue)
                    {
                        bool booleanValue = allSelected.Value;
                        foreach (ElementModel model in viewData)
                        {
                            model.IsSelected = booleanValue;
                        }
                    }
                }
            }
        }


        private ObservableCollection<ElementModel> modelData = null;
        public ObservableCollection<ElementModel> ElementModelData
        {
            get => modelData;
            set
            {
                if (SetProperty(ref modelData, value) && modelData != null)
                {
                    ViewDataCollection = CollectionViewSource.GetDefaultView(modelData) as ListCollectionView;
                    UniqueLevelNames = new SortedSet<string>(modelData.Select(m => m.LevelName).Append(string.Empty)).ToList();
                    UniqueSymbolNames = new SortedSet<string>(modelData.Select(m => m.SymbolName).Append(string.Empty)).ToList();
                }
            }
        }


        private ListCollectionView viewData = null;
        public ListCollectionView ViewDataCollection
        {
            get => viewData;
            set
            {
                if (SetProperty(ref viewData, value))
                {
                    AllSelectChecked = false;
                    ReviewDataViewCollection();
                    VerifySelectDataViewCollection();
                }
            }
        }


        private void ReviewDataViewCollection()
        {
            if (viewData != null && !viewData.IsEmpty)
            {
                using (viewData.DeferRefresh())
                {
                    viewData.SortDescriptions.Clear();
                    viewData.GroupDescriptions.Clear();
                    viewData.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.FamilyName)));
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.SymbolName), ListSortDirection.Ascending));
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.MinSizeInMm), ListSortDirection.Ascending));
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.IsSelected), ListSortDirection.Descending));
                }
            }
        }


        internal void VerifySelectDataViewCollection()
        {
            if (viewData != null && !viewData.IsEmpty)
            {
                currentItem = ViewDataCollection.GetItemAt(0);
                ViewDataCollection.Filter = FilterModelCollection;
                if (currentItem is ElementModel model && viewData.MoveCurrentTo(currentItem))
                {
                    IEnumerable<ElementModel> items = viewData.OfType<ElementModel>();
                    AllSelectChecked = items.All(x => x.IsSelected == model.IsSelected) ? model.IsSelected : null;
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
                    ViewDataCollection.Filter = FilterModelCollection;
                    if (!string.IsNullOrEmpty(levelText))
                    {
                        ActivatePlanViewByLevel();
                        AllSelectChecked = false;
                    }
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
                    ViewDataCollection.Filter = FilterModelCollection;
                    if (!string.IsNullOrEmpty(symbolText))
                    {
                        ActivatePlanViewByLevel();
                        AllSelectChecked = false;
                    }
                }
            }
        }


        private IList<string> levelNames = null;
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
            return obj is not null && obj is ElementModel model
            && model.SymbolName.Equals(symbolText)
            && model.LevelName.Equals(levelText);
        }


        private void ActivatePlanViewByLevel()
        {
            if (viewData != null && !viewData.IsEmpty)
            {
                currentItem = viewData.GetItemAt(0);
                if (currentItem is ElementModel model)
                {
                    Task task = RevitTask.RunAsync(app =>
                    {
                        UIDocument uidoc = app.ActiveUIDocument;
                        ViewPlan view = RevitViewManager.GetPlanView(uidoc, model.HostLevel);
                        RevitViewManager.ActivateView(uidoc, view, ViewDiscipline.Mechanical);
                    });
                }
            }
        }

        #endregion


        #region RefreshDataCommand
        public ICommand RefreshDataCommand { get; private set; }
        private void RefreshActiveDataHandler()
        {
            IsDataRefresh = false;
            if (document != null && material != null && category != null)
            {
                Task task = Task.WhenAll();
                task = task.ContinueWith(_ =>
                {
                    IsOptionEnabled = false;
                    IsDataRefresh = true;
                }, taskContext);
            }
        }

        #endregion


        #region ShowCollisionCommand

        public ICommand ShowCollisionCommand { get; private set; }
        private async Task ShowHandelCommandAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                dialogResult = null;
                ViewDataCollection?.Refresh();
                if (0 < ViewDataCollection?.Count)
                {
                    doc = app.ActiveUIDocument.Document;
                    UIDocument uidoc = app.ActiveUIDocument;
                    currentItem = ViewDataCollection.GetItemAt(0);
                    if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                    {
                        patternId ??= RevitViewManager.GetSolidFillPatternId(doc);
                        if (previewControl is null && currentItem is ElementModel model && model.IsValidModel())
                        {
                            if (RevitViewManager.SetCustomSectionBox(uidoc, model.SectionPlane.Origin, view3d))
                            {
                                uidoc.Selection.SetElementIds(new List<ElementId> { model.Instanse.Id });
                                RevitViewManager.SetCustomColor(uidoc, view3d, patternId, model.Instanse);
                                RevitViewManager.ShowModelInPlanView(uidoc, model, ViewDiscipline.Mechanical);
                                previewControl = SmartToolApp.ServiceProvider.GetRequiredService<PreviewControlModel>();
                                previewControl.ShowPreviewControl(app, view3d);
                            }
                        }
                    }
                }
            });
        }

        #endregion


        #region OkCanselCommand

        public ICommand OkCanselCommand { get; private set; }
        private async Task OkCanselHandelCommandAsync()
        {
            if (dialogResult.HasValue)
            {
                await RevitTask.RunAsync(app =>
                {
                    doc = app.ActiveUIDocument.Document;
                    if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                    {
                        if (currentItem is ElementModel model && model.IsValidModel())
                        {
                            if (dialogResult.Value && ElementModelData.Remove(model))
                            {
                                collisionManager.VerifyOpenningSize(doc, model);
                                collisionManager.CreateOpening(doc, model, wallOpenning);
                            }
                            else
                            {
                                model.IsSelected = false;
                                ViewDataCollection.Remove(model);
                                ViewDataCollection.AddNewItem(model);
                                ViewDataCollection.CommitNew();    
                            }
                        }
                    }
                });
            }
        }

        #endregion


        #region PreviewControl

        private View3D view3d { get; set; } = null;
        private ElementId patternId { get; set; } = null;
        private PreviewControlModel previewControl { get; set; } = null;

        private bool? dialogResult = false;
        public bool? DialogResult
        {
            get => dialogResult;
            set
            {
                if (SetProperty(ref dialogResult, value))
                {
                    if (dialogResult.HasValue)
                    {
                        previewControl = null;
                    }
                }
            }
        }

        #endregion


        // Алгоритм проверки семейств отверстия
        /*
        * Проверить семейство отверстий правильно ли они расположены
        * Найти все семейства и определить пересекается ли оно с чем либо (по краю)
        * Если не пересекается проверить есть ли по центру элемент если нет то удалить
        * Если пересекается то удалить
        */


        // Общий алгоритм проверки пользователем элементов
        /*
         * Объединения элементов в одном месте в один большой solid если они пересекаются
         * Объединения проема если пересекаются solid или находятся очень близко (можно по точке или bbox создать)
         * Создать новое семейство проема с возможностью изменения размеров => CutOffset сохраняется
         * Реализовать автосинхронизацию при окончание выполнение или изменения проекта
         */


        public void Dispose()
        {
            ClearAndResetData();
            collisionManager?.Dispose();
        }
    }

}