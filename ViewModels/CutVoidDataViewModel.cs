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
        public CutVoidDockPaneView DockPanelView { get; set; }
        private static SynchronizationContext context { get; set; }
        public static ExternalEvent RevitExternalEvent { get; set; }

        private static readonly AutoResetEvent manualResetEvent = new(true);
        private readonly string docUniqueId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly TaskScheduler taskContext = TaskScheduler.FromCurrentSynchronizationContext();
        private readonly RevitPurginqManager constructManager = SmartToolApp.ServiceProvider.GetRequiredService<RevitPurginqManager>();
        private readonly CutVoidCollisionManager collisionManager = SmartToolApp.ServiceProvider.GetRequiredService<CutVoidCollisionManager>();


        public CutVoidDataViewModel(APIEventHandler eventHandler)
        {
            RevitExternalEvent = ExternalEvent.Create(eventHandler);
            RefreshDataCommand = new AsyncRelayCommand(RefreshActiveDataHandler);
            ShowCollisionCommand = new AsyncRelayCommand(ShowHandelCommandAsync);
            OkCanselCommand = new AsyncRelayCommand(OkCanselHandelCommandAsync);
        }


        #region Templory

        private Document doc { get; set; } = null;
        private View3D view3d { get; set; } = null;
        private ElementId patternId { get; set; } = null;
        private ElementModel current { get; set; } = null;


        private PreviewControlModel control = null;
        private bool? dialogResult = false;
        public bool? DialogResult
        {
            get => dialogResult;
            set
            {
                if (SetProperty(ref dialogResult, value))
                {
                    control = null;
                }
            }
        }

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
                    FamilySymbols = null;

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


        internal async void GetElementInView(Element elem)
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    RevitViewManager.ShowElement(app.ActiveUIDocument, elem);
                    System.Windows.Clipboard.SetText(elem.Id.ToString());
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
                if (SetProperty(ref isSelected, value))
                {
                    if (dataView != null && value.HasValue)
                    {
                        bool booleanValue = value.Value;
                        foreach (ElementModel model in dataView)
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
                    UniqueLevelNames = new SortedSet<string>(collection.Select(m => m.LevelName).Append(string.Empty)).ToList();
                    UniqueSymbolNames = new SortedSet<string>(collection.Select(m => m.SymbolName).Append(string.Empty)).ToList();
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
                    if (!dataView.IsEmpty)
                    {
                        SortDataViewCollection();
                        VerifyAllSelectedData();
                    }
                    else
                    {
                        IsAllSelectChecked = false;
                        current = null;
                    }
                }
            }
        }

        private void SortDataViewCollection()
        {
            using (dataView.DeferRefresh())
            {
                dataView.SortDescriptions.Clear();
                dataView.GroupDescriptions.Clear();
                dataView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.IsSelected)));
                dataView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.FamilyName)));
                dataView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.LevelName), ListSortDirection.Ascending));
                dataView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.SymbolName), ListSortDirection.Ascending));
                dataView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.IsSelected), ListSortDirection.Descending));
            }
        }

        internal void VerifyAllSelectedData()
        {
            if (dataView.IsInUse && !dataView.IsEmpty && !dataView.NeedsRefresh)
            {
                IEnumerable<ElementModel> items = dataView.OfType<ElementModel>();
                ElementModel firstItem = dataView.OfType<ElementModel>().FirstOrDefault();
                IsAllSelectChecked = items.All(x => x.IsSelected == firstItem.IsSelected) ? firstItem.IsSelected : null;
                current = dataView.GetItemAt(0) as ElementModel;
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
                    ShowPlanViewAsync();
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
                    ZoomPlanViewAsync();
                }
            }
        }


        private async void ShowPlanViewAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    if (current is ElementModel model)
                    {
                        ViewPlan view = RevitViewManager.CreatePlan(doc, model.HostLevel);
                        RevitViewManager.ShowView(app.ActiveUIDocument, view);
                    }
                }
            });
        }


        private async void ZoomPlanViewAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    List<ElementId> ids = new(dataView.Count);
                    if (current is ElementModel model)
                    {
                        ElementId levelId = model.HostLevel.Id;
                        foreach (ElementModel mdl in dataView)
                        {
                            if (levelId.Equals(mdl.HostLevel.Id))
                            {
                                ids.Add(mdl.Instanse.Id);
                            }
                        }
                        RevitViewManager.ShowElements(app.ActiveUIDocument, ids);
                    }
                }
            });
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
            return obj is ElementModel model
            && ((string.IsNullOrEmpty(LevelTextFilter) && string.IsNullOrEmpty(SymbolTextFilter))
            || (model.LevelName.Contains(LevelTextFilter) && model.SymbolName.Contains(SymbolTextFilter))
            || (model.LevelName.Equals(LevelTextFilter, StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(model.SymbolName))
            || (model.SymbolName.Equals(SymbolTextFilter, StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(model.LevelName)));
        }

        #endregion


        #region ResetCurrentContext

        //private void ResetCurrentContext()
        //{
        //    context = DataViewCollection?.SourceCollection as SynchronizationContext;
        //    if (context != null && SynchronizationContext.Current != context)
        //    {
        //        try
        //        {
        //            SynchronizationContext.SetSynchronizationContext(context);
        //            Logger.ThreadProcessLog("Complited: " + nameof(ResetCurrentContext));
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error(nameof(ResetCurrentContext) + ex.Message);
        //        }
        //    }
        //}

        #endregion


        #region RefreshDataCommand
        public ICommand RefreshDataCommand { get; private set; }
        private async Task RefreshActiveDataHandler()
        {
            IsDataRefresh = false;
            if (document != null && material != null && category != null)
            {
                await Task.Delay(1000).ContinueWith(_ =>
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
                DialogResult = null;
                doc = app.ActiveUIDocument.Document;
                UIDocument uidoc = app.ActiveUIDocument;
                if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                {
                    if (current is ElementModel model && model.IsSelected && control == null)
                    {
                        if (RevitViewManager.SetCustomSectionBox(uidoc, model.Origin, view3d))
                        {
                            patternId ??= RevitViewManager.GetSolidFillPatternId(doc);
                            RevitViewManager.SetCustomColor(uidoc, view3d, patternId, model.Instanse);
                            control = SmartToolApp.ServiceProvider.GetRequiredService<PreviewControlModel>();
                            control.ShowPreviewControl(app, view3d);
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
                        if (current is ElementModel model && model.IsSelected)
                        {
                            if (dialogResult.Value && ElementModelData.Remove(current))
                            {
                                collisionManager.CreateOpening(doc, model, wallOpenning, floorOpenning);
                            }
                            else
                            {
                                current.IsSelected = false;
                                DataViewCollection.Refresh();
                                object item = DataViewCollection.GetItemAt(0);
                                if (item != null && item is ElementModel elementModel)
                                {
                                    current = elementModel;
                                }
                            }
                        }
                    }
                });
            }
        }

        #endregion


        // Алгоритм проверки семейств отверстия
        /*
        * Проверить семейство что это реальное отверстие
        * Найти все семейства и определить пересекается ли оно с чем либо (по краю)
        * Если не пересекается проверить есть ли по центру элемент если нет то удалить
        * Если пересекается то удалить
        */


        // Общий алгоритм проверки пользователем елементов
        /*
         * Объединения элементов в одном месте в один большой solid если они пересекаются
         * Объединения проема если пересекаются solid или находятся очень близко (можно по точке или bbox создать)
         * Создать новое семейство проема с возможностью изменения размеров => CutOffset сохраняется
         * Реализовать автосинхронизацию при окончание выполнение или изменения проекта
         * Кнопки = (показать/создать/остановить)
         */


        public void Dispose()
        {
            ClearAndResetData();
            collisionManager?.Dispose();
        }
    }

}