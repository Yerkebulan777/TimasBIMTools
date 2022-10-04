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
    public sealed class CutOpeningDataViewModel : ObservableObject, IDisposable
    {
        public CutOpeningDockPanelView DockPanelView { get; set; } = null;

        private Task task { get; set; } = null;
        private View3D view3d { get; set; } = null;
        private IList<Element> elements { get; set; } = null;
        private IDictionary<int, ElementId> constructionTypeIds { get; set; } = null;
        private IDictionary<string, FamilySymbol> familySymbols { get; set; } = null;
        private CancellationToken cancelToken { get; set; } = CancellationToken.None;

        private readonly object syncLocker = new();
        private readonly string documentId = Properties.Settings.Default.ActiveDocumentUniqueId;
        private readonly CutOpeningCollisionManager manager = SmartToolController.Services.GetRequiredService<CutOpeningCollisionManager>();
        private readonly CutOpeningStartExternalHandler viewHandler = SmartToolController.Services.GetRequiredService<CutOpeningStartExternalHandler>();


        public CutOpeningDataViewModel()
        {
            viewHandler.Completed += OnContextHandlerCompleted;
            ShowSettingsCommand = new RelayCommand(ShowSettingsHandelCommand);
            ShowExecuteCommand = new AsyncRelayCommand(ExecuteHandelCommandAsync);
            SelectItemCommand = new RelayCommand(SelectAllVaueHandelCommand);
            CanselCommand = new RelayCommand(CancelCallbackLogic);
        }


        [STAThread]
        private void OnContextHandlerCompleted(object sender, BaseCompletedEventArgs args)
        {
            IsStarted = true;
            viewHandler.Completed -= OnContextHandlerCompleted;
            DocModelCollection = args.DocumentModels.ToObservableCollection();
            constructionTypeIds = args.ConstructionTypeIds;
        }


        public ICommand ShowSettingsCommand { get; private set; }

        [STAThread]
        private void ShowSettingsHandelCommand()
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                Document doc = null;
                ElementModelData.Clear();
                task = !IsOptionsEnabled
                    ? RevitTask.RunAsync(async app =>
                    {
                        IsDataEnabled = false;
                        IsOptionsEnabled = true;
                        doc = app.ActiveUIDocument.Document;
                        await Task.Delay(1000).ConfigureAwait(true);
                    })
                    .ContinueWith(app =>
                    {
                        if (documentId.Equals(doc.ProjectInformation.UniqueId))
                        {
                            EngineerCategories = RevitFilterManager.GetEngineerCategories(doc);
                            DockPanelView.ComboStructureMats.ItemsSource = RevitFilterManager.GetConstructionCoreMaterials(doc, constructionTypeIds);
                            familySymbols = RevitFilterManager.GetHostedFamilySymbols(doc, BuiltInCategory.OST_GenericModel);
                            DockPanelView.ComboRectangSymbol.ItemsSource = familySymbols;
                            DockPanelView.ComboRoundedSymbol.ItemsSource = familySymbols;
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext())
                    : RevitTask.RunAsync(async app =>
                    {
                        IsDataEnabled = true;
                        IsOptionsEnabled = false;
                        doc = app.ActiveUIDocument.Document;
                        await Task.Delay(1000).ConfigureAwait(true);
                    })
                    .ContinueWith(app =>
                    {
                        if (documentId.Equals(doc.ProjectInformation.UniqueId))
                        {
                            DockPanelView.ComboLevelFilter.ItemsSource = RevitFilterManager.GetValidLevels(doc);
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
            });
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
                    GetGeneral3DView();
                }
            }
        }


        private bool enabledOptions = false;
        public bool IsOptionsEnabled
        {
            get => enabledOptions;
            set
            {
                if (value != enabledOptions)
                {
                    if (SetProperty(ref enabledOptions, value))
                    {
                        IsDataEnabled = !enabledOptions;
                    }
                }
            }
        }


        private bool enabledData = false;
        public bool IsDataEnabled
        {
            get => enabledData;
            set
            {
                if (value == false || (docModel != null && category != null))
                {
                    if (SetProperty(ref enabledData, value))
                    {
                        IsOptionsEnabled = !enabledData;
                    }
                }
            }
        }

        #endregion


        #region Set settings

        private ObservableCollection<DocumentModel> docModels = null;
        public ObservableCollection<DocumentModel> DocModelCollection
        {
            get => docModels;
            set
            {
                if (SetProperty(ref docModels, value))
                {
                    SelectedDocModel = docModels.FirstOrDefault();
                }
            }
        }


        private DocumentModel docModel = null;
        public DocumentModel SelectedDocModel
        {
            get => docModel;
            set => SetProperty(ref docModel, value);
        }


        private IDictionary<string, Category> categories;
        public IDictionary<string, Category> EngineerCategories
        {
            get => categories;
            set => SetProperty(ref categories, value);
        }


        private Category category = null;
        public Category SelectedCategory
        {
            get => category;
            set => SetProperty(ref category, value);
        }


        private IDictionary<string, Material> structMats;
        public IDictionary<string, Material> StructureMaterials
        {
            get => structMats;
            set => SetProperty(ref structMats, value);
        }


        private Material material;
        public Material SelectedMaterial
        {
            get => material;
            set
            {
                if (SetProperty(ref material, value) && value != null)
                {
                    GetInstancesByMaterial(material.Name);
                }
            }
        }



        private FamilySymbol rectangle;
        public FamilySymbol RectangSymbol
        {
            get => rectangle;
            set
            {
                if (SetProperty(ref rectangle, value))
                {
                    ActivateFamilySimbol(rectangle);
                }
            }
        }


        private FamilySymbol rounded;
        public FamilySymbol RoundedSymbol
        {
            get => rounded;
            set
            {
                if (SetProperty(ref rounded, value))
                {
                    ActivateFamilySimbol(rounded);
                }
            }
        }


        private double minSize = Properties.Settings.Default.MinSideSizeInMm;
        public double MinSideSize
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


        private double maxSize = Properties.Settings.Default.MaxSideSizeInMm;
        public double MaxSideSize
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


        private double cutOffset = Properties.Settings.Default.CutOffsetInMm;
        public double CutOffsetSize
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


        #region Set filter data

        private Level level = null;
        public Level SearchLevel
        {
            get => level;
            set
            {
                if (SetProperty(ref level, value) && value != null)
                {
                    Properties.Settings.Default.Upgrade();
                    SnoopIntersectionDataByLevel(level);
                }
            }
        }

        #endregion


        #region Methods

        [STAThread]
        private void GetGeneral3DView()
        {
            task = RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                if (documentId.Equals(doc.ProjectInformation.UniqueId))
                {
                    view3d = RevitViewManager.Get3dView(app.ActiveUIDocument);
                }
            });
        }


        [STAThread]
        private void GetInstancesByMaterial(string materialName)
        {
            task = RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                if (documentId.Equals(doc.ProjectInformation.UniqueId))
                {
                    elements = RevitFilterManager.GetInstancesByCoreMaterial(doc, constructionTypeIds, materialName);
                }
            });
        }


        [STAThread]
        private void SnoopIntersectionDataByLevel(Level level)
        {
            IEnumerable<ElementModel> data = null;
            task = RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                if (documentId.Equals(doc.ProjectInformation.UniqueId))
                {
                    data = manager.GetCollisionByLevel(doc, level, docModel, category, elements);
                }
            }).ContinueWith(app =>
            {
                ElementModelData = data.ToObservableCollection();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }


        private void ActivateFamilySimbol(FamilySymbol symbol)
        {
            if (symbol != null && !symbol.IsActive)
            {
                symbol.Activate();
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


        private ICollectionView dataView = new ListCollectionView(new List<ElementModel>());
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


        #region TextFilter

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
                if (documentId.Equals(doc.ProjectInformation.UniqueId))
                {
                    foreach (ElementModel model in DataViewCollection)
                    {
                        if (model.IsSelected && ElementModelData.Remove(model))
                        {
                            Element elem = doc.GetElement(new ElementId(model.IdInt));
                            lock (syncLocker)
                            {
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
            lock (syncLocker)
            {
                try
                {
                    cts.Cancel(true);
                    cancelToken = cts.Token;
                }
                catch (AggregateException)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        task = Task.Delay(1000).ContinueWith((action) => Logger.Warning("Task cansceled"));
                    }
                }
            }
        }
        #endregion


        [STAThread]
        public void Dispose()
        {
            manager?.Dispose();
            ElementModelData.Clear();
            if (DataViewCollection is ListCollectionView list)
            {
                foreach (object item in list)
                {
                    list.Remove(item);
                }
            }
        }
    }
}