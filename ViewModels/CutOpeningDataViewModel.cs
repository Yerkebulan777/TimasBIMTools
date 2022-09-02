using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
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
        public Document CurrentDocument { get; internal set; } = null;
        public CutOpeningDockPanelView DockPanelView { get; set; } = null;
        public static CancellationToken CancelToken { get; set; } = CancellationToken.None;

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
            CloseCommand = new RelayCommand(CancelCallbackLogic);
        }


        #region INotifyPropertyChanged members

        private bool enable = false;
        public bool IsCollectionEnabled
        {
            get => enable;
            set => SetProperty(ref enable, value);
        }

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
            return new SortedSet<string>(collection.Select(c => c.SymbolName)).ToList();
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
                if (CurrentDocument.Equals(doc))
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


        #region ExecuteCommand
        public ICommand ShowExecuteCommand { get; private set; }

        [STAThread]
        private async Task ExecuteHandelCommandAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                View3D view3d = DockPanelView.View3d;
                UIDocument uidoc = app.ActiveUIDocument;
                Document doc = app.ActiveUIDocument.Document;
                if (CurrentDocument.Equals(doc) && !ViewCollection.IsEmpty)
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
                    // boolSet to buttom IsCollectionEnabled
                    UniqueElementNames = GetUniqueStringList(RevitElementModels);
                }
            });
            
        }

        #endregion


        #region CloseCommand
        public ICommand CloseCommand { get; private set; }


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
                        _ = Task.Delay(1000).ContinueWith((action) => RevitLogger.Warning("Task cansceled"));
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