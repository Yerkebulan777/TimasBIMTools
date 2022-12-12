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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Document = Autodesk.Revit.DB.Document;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutVoidDataViewModel : ObservableObject
    {
        public CutVoidDockPaneView DockPanelView { get; set; }
        public static ExternalEvent RevitExternalEvent { get; set; }

        private readonly string localPath = SmartToolHelper.LocalPath;
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
        //private IList<Family> families= null;

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
                        GetFamilySymbolsToData();
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
            set => SetProperty(ref document, value);
        }


        private Material material = null;
        public Material SelectedMaterial
        {
            get => material;
            set => SetProperty(ref material, value);
        }


        private Category category = null;
        public Category SelectedCategory
        {
            get => category;
            set => SetProperty(ref category, value);
        }


        private ICollection<DocumentModel> documents = null;
        public ICollection<DocumentModel> DocumentCollection
        {
            get => documents;
            set
            {
                if (SetProperty(ref documents, value) && documents != null)
                {
                    SelectedDocument = documents.FirstOrDefault();
                }
            }
        }


        private IDictionary<string, Material> structs = null;
        public IDictionary<string, Material> StructureMaterials
        {
            get => structs;
            set => SetProperty(ref structs, value);
        }


        private IDictionary<string, Category> categos = null;
        public IDictionary<string, Category> EngineerCategories
        {
            get => categos;
            set => SetProperty(ref categos, value);
        }

        #endregion


        #region FamilySymbols

        private IList<FamilySymbol> symbols;
        public IList<FamilySymbol> FamilySymbolList
        {
            get => symbols;
            set
            {
                if (symbols != null && symbols.Count > 0)
                {
                    value = value.Union(symbols).ToList();
                }
                if (SetProperty(ref symbols, value) && symbols != null)
                {
                    Logger.Info("Output: " + symbols.Count.ToString());
                }
            }
        }


        public async void LoadFamilyAsync(string familyPath)
        {
            FamilySymbolList = await RevitTask.RunAsync(async app =>
            {
                using Transaction trx = new(doc);
                IList<FamilySymbol> result = null;
                doc = app.ActiveUIDocument.Document;
                TransactionStatus status = trx.Start("LoadFamily");
                if (status == TransactionStatus.Started)
                {
                    IFamilyLoadOptions opt = UIDocument.GetRevitUIFamilyLoadOptions();
                    if (doc.LoadFamily(familyPath, opt, out Family family))
                    {
                        status = trx.Commit();
                        result = GetFamilySymbolData(family);
                        Document familyDoc = doc.EditFamily(family);
                        if (familyDoc != null && familyDoc.IsFamilyDocument)
                        {
                            GetFamilySharedParameterData(familyDoc);
                            string familyPath = @$"{localPath}\{family.Name}.rfa";
                            if (File.Exists(familyPath)) { File.Delete(familyPath); }
                            familyDoc.SaveAs(familyPath, new SaveAsOptions
                            {
                                OverwriteExistingFile = true,
                                MaximumBackups = 3,
                                Compact = true,
                            });
                            if (familyDoc.Close(false))
                            {
                                await Task.Yield();
                            }
                        }
                    }
                    else if (!trx.HasEnded())
                    {
                        status = trx.RollBack();
                    }
                }
                return result;
            });
        }


        private string[] ProcessDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Logger.Error("Not found directory path: " + directory);
            }
            return Directory.GetFiles(directory, "*.rfa", SearchOption.TopDirectoryOnly);
        }


        private IList<FamilySymbol> GetFamilySymbolData(Family family)
        {
            IList<FamilySymbol> result = new List<FamilySymbol>(5);
            if (family != null && family.IsValidObject && family.IsEditable)
            {
                foreach (ElementId symbId in family.GetFamilySymbolIds())
                {
                    Element element = doc.GetElement(symbId);
                    if (element is FamilySymbol symbol)
                    {
                        result.Add(symbol);
                    }
                }
            }
            return result;
        }


        internal void ActivateFamilySimbol(FamilySymbol symbol)
        {
            Task task = RevitTask.RunAsync(app =>
            {
                using Transaction trx = new(symbol.Document);
                if (symbol.IsValidObject && !symbol.IsActive)
                {
                    _ = trx.Start("Activate family");
                    symbol.Activate();
                    _ = trx.Commit();
                }
            });
        }

        #endregion


        #region ParameterData

        private IDictionary<string, Guid> paramData = null;
        public IDictionary<string, Guid> SharedParameterData
        {
            get => paramData;
            set => SetProperty(ref paramData, value);
        }


        private void GetFamilySharedParameterData(Document familyDoc)
        {
            FamilyManager familyManager = familyDoc.FamilyManager;
            SharedParameterData ??= new SortedList<string, Guid>(10);
            foreach (FamilyParameter param in familyManager.GetParameters())
            {
                if (param.UserModifiable && param.IsInstance)
                {
                    if (!param.IsReadOnly && param.IsShared)
                    {
                        if (!param.IsDeterminedByFormula)
                        {
                            string name = param.Definition.Name;
                            SharedParameterData[name] = param.GUID;
                        }
                    }
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
                    SharedParameterData = null;
                    FamilySymbolList = null;
                    ElementModelData = null;
                    SymbolTextFilter = null;
                    LevelTextFilter = null;
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


        private async void GetFamilySymbolsToData()
        {
            await RevitTask.RunAsync(app =>
            {
                if (symbols == null || symbols.Count > 0)
                {
                    foreach (string familyPath in ProcessDirectory(localPath))
                    {
                        LoadFamilyAsync(familyPath);
                    }
                }
            });
        }


        private async void SnoopIntersectionByInputData()
        {
            if (document != null && material != null && category != null)
            {
                ElementModelData = await RevitTask.RunAsync(app =>
                {
                    doc = app.ActiveUIDocument.Document;
                    return collisionManager.GetCollisionByInputData(doc, document, material, category).ToObservableCollection();
                });
            }
        }


        internal void ShowElementModelView(ElementModel model)
        {
            if (model != null && model.Instanse.IsValidObject)
            {
                Task task = RevitTask.RunAsync(app =>
                {
                    doc = app.ActiveUIDocument.Document;
                    UIDocument uidoc = app.ActiveUIDocument;
                    if (docUniqueId.Equals(doc.ProjectInformation.UniqueId))
                    {
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
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.IsSelected), ListSortDirection.Descending));
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.SizeInMm), ListSortDirection.Ascending));
                    viewData.Filter = FilterModelCollection;
                }
            }
        }


        internal void VerifySelectDataViewCollection()
        {
            if (viewData != null && !viewData.IsEmpty)
            {
                currentItem = viewData.GetItemAt(0);
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
            return obj is ElementModel model
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
                        doc = app.ActiveUIDocument.Document;
                        UIDocument uidoc = app.ActiveUIDocument;
                        Level level = doc.GetElement(model.Host.LevelId) as Level;
                        ViewPlan view = RevitViewManager.GetPlanViewByLevel(uidoc, level);
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
                                collisionManager.CreateOpening(doc, model);
                            }
                            else
                            {
                                model.IsSelected = false;
                                ViewDataCollection.Remove(model);
                                _ = ViewDataCollection.AddNewItem(model);
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