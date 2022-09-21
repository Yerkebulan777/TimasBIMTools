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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Document = Autodesk.Revit.DB.Document;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutOpeningDataViewModel : ObservableObject, IDisposable
    {
        public CutOpeningDockPanelView DockPanelView { get; set; } = null;
        public string DocumentGuid { get; internal set; } = string.Empty;
        public static CancellationToken CancelToken { get; set; } = CancellationToken.None;

        private string guid = null;
        private readonly object syncLocker = new object();
        private IList<ElementModel> collection = new List<ElementModel>();
        private readonly string roundOpeningId = Properties.Settings.Default.RoundSymbolUniqueId;
        private readonly string rectangOpeningId = Properties.Settings.Default.RectangSymbolUniqueId;
        private readonly CutOpeningCollisionManager manager = SmartToolController.Services.GetRequiredService<CutOpeningCollisionManager>();


        public CutOpeningDataViewModel()
        {
            SnoopCommand = new AsyncRelayCommand(SnoopHandelCommandAsync);
            ShowExecuteCommand = new AsyncRelayCommand(ExecuteHandelCommandAsync);
            SelectItemCommand = new RelayCommand(SelectAllVaueHandelCommand);
            CanselCommand = new RelayCommand(CancelCallbackLogic);
        }


        private Level level = null;
        public Level SelectedLevel
        {
            get => level;
            set
            {
                if (SetProperty(ref level, value) && level != null)
                {
                    Properties.Settings.Default.CurrentLevelUniqueId = level.UniqueId;
                    Properties.Settings.Default.Save();
                }
            }
        }


        #region Visibility

        private bool isEnabledOptions = false;
        public bool IsOptionsEnabled
        {
            get => isEnabledOptions;
            set
            {
                if (SetProperty(ref isEnabledOptions, value))
                {
                    IsDataEnabled = !isEnabledOptions;
                }
            }
        }


        private bool isEnabledData = false;
        public bool IsDataEnabled
        {
            get => isEnabledData;
            set => SetProperty(ref isEnabledData, value);
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
                        foreach (ElementModel model in ViewCollection)
                        {
                            model.IsSelected = val;
                        }
                    }

                }
            }
        }


        private ObservableCollection<DocumentModel> docModels = new ObservableCollection<DocumentModel>();
        public ObservableCollection<DocumentModel> DocumentModels
        {
            get => docModels;
            set => SetProperty(ref docModels, value);
        }


        private ObservableCollection<ElementModel> modelCollection = new ObservableCollection<ElementModel>();
        public ObservableCollection<ElementModel> RevitElementModels
        {
            get => modelCollection;
            set
            {
                if (SetProperty(ref modelCollection, value))
                {
                    ViewCollection = CollectionViewSource.GetDefaultView(value);
                    UniqueElementNames = GetUniqueStringList(value);
                }
            }
        }


        private ICollectionView viewCollect = new CollectionView(new List<ElementModel>());
        public ICollectionView ViewCollection
        {
            get => viewCollect;
            set
            {
                if (SetProperty(ref viewCollect, value))
                {
                    ViewCollection.SortDescriptions.Clear();
                    ViewCollection.GroupDescriptions.Clear();
                    ViewCollection.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.CategoryName)));
                    ViewCollection.SortDescriptions.Add(new SortDescription(nameof(ElementModel.SymbolName), ListSortDirection.Ascending));
                    ViewCollection.SortDescriptions.Add(new SortDescription(nameof(ElementModel.Description), ListSortDirection.Ascending));
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
                    ViewCollection.Filter = FilterModelCollection;
                    SelectAllVaueHandelCommand();
                    ViewCollection.Refresh();
                }
            }
        }

        private IList<string> uniqueNames = null;
        public IList<string> UniqueElementNames
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
            || !(obj is ElementModel model) || model.SymbolName.Contains(FilterText)
            || model.SymbolName.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase)
            || model.SymbolName.Equals(FilterText, StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion


        #region SnoopCommand

        public ICommand SnoopCommand { get; private set; }
        private async Task SnoopHandelCommandAsync()
        {
            RevitElementModels?.Clear();
            View3D view3d = DockPanelView.View3d;
            RevitElementModels = await RevitTask.RunAsync(app =>
            {
                Document doc = app.ActiveUIDocument.Document;
                UIDocument uidoc = app.ActiveUIDocument;
                guid = doc.ProjectInformation.UniqueId;
                if (DocumentGuid.Equals(guid))
                {
                    manager.InitializeActiveDocument(doc);
                    ActivateFamilySimbol(doc, roundOpeningId);
                    ActivateFamilySimbol(doc, rectangOpeningId);
                    collection = manager.GetCollisionCommunicateElements();
                }
                RevitViewManager.Show3DView(uidoc, view3d);
                return collection.ToObservableCollection();
            });
        }


        private void ActivateFamilySimbol(Document doc, string simbolId)
        {
            if (!string.IsNullOrEmpty(simbolId))
            {
                Element element = doc.GetElement(simbolId);
                if (element is FamilySymbol symbol && !symbol.IsActive)
                {
                    symbol.Activate();
                }
            }
        }

        #endregion


        #region SelectItemCommand
        public ICommand SelectItemCommand { get; private set; }
        private void SelectAllVaueHandelCommand()
        {
            IEnumerable<ElementModel> items = ViewCollection.OfType<ElementModel>();
            ElementModel firstItem = ViewCollection.OfType<ElementModel>().FirstOrDefault();
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
                View3D view3d = DockPanelView.View3d;
                UIDocument uidoc = app.ActiveUIDocument;
                Document doc = app.ActiveUIDocument.Document;
                guid = doc.ProjectInformation.UniqueId;
                if (DocumentGuid.Equals(guid) && !ViewCollection.IsEmpty)
                {
                    foreach (ElementModel model in ViewCollection)
                    {
                        if (model.IsSelected && RevitElementModels.Remove(model))
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
                    UniqueElementNames = GetUniqueStringList(RevitElementModels);
                }
            });

        }

        #endregion


        #region CloseCommand
        public ICommand CanselCommand { get; private set; }


        private void CancelCallbackLogic()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            lock (syncLocker)
            {
                try
                {
                    cts.Cancel(true);
                    CancelToken = cts.Token;
                }
                catch (AggregateException)
                {
                    if (CancelToken.IsCancellationRequested)
                    {
                        _ = Task.Delay(1000).ContinueWith((action) => Logger.Warning("Task cansceled"));
                    }
                }
            }
        }
        #endregion


        public void Dispose()
        {
            manager?.Dispose();
            collection?.Clear();
            RevitElementModels.Clear();
            FilterText = string.Empty;
        }
    }
}