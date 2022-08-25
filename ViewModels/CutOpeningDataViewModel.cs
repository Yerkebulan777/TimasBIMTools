using Autodesk.Revit.DB;
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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutOpeningDataViewModel : ObservableObject, IDisposable
    {
        public DockPanelPage DockPanelView { get; set; } = null;
        public static CancellationToken CancelToken = CancellationToken.None;

        private readonly object syncLocker = new object();
        private readonly ElementId elementId = ElementId.InvalidElementId;
        private IList<RevitElementModel> collection = new List<RevitElementModel>(150);
        private readonly int roundOpeningId = Properties.Settings.Default.RoundOpeningSimbolIdInt;
        private readonly int rectangOpeningId = Properties.Settings.Default.RectanOpeningSimbolIdInt;
        private readonly CutOpeningWindows openingView = SmartToolController.Services.GetRequiredService<CutOpeningWindows>();
        private readonly CutOpeningCollisionDetection manager = SmartToolController.Services.GetRequiredService<CutOpeningCollisionDetection>();


        public CutOpeningDataViewModel()
        {
            CloseCommand = new RelayCommand(CancelCallbackLogic);
            SetFilterCommand = new RelayCommand(SetFilterTextCommand);
            CleanFilterCommand = new RelayCommand(СleanFilterTextCommand);
            ApplyCommand = new AsyncRelayCommand(ExecuteApplyCommandAsync);
            SnoopCommand = new AsyncRelayCommand(ExecuteSnoopCommandAsync);
            SelectAllCommand = new RelayCommand<bool?>(HandleSelectAllCommand);
        }


        #region INotifyPropertyChanged members

        private bool enable = false;
        public bool IsEnabled
        {
            get => enable;
            set => SetProperty(ref enable, value);
        }


        private Document doc = null;
        public Document CurrentDocument
        {
            get => doc;
            set
            {
                if (value != null)
                {
                    doc = value;
                    OnPropertyChanged(nameof(CurrentDocument));
                    CommandManager.InvalidateRequerySuggested();
                };
            }
        }


        private ICollectionView collectionView;
        public ICollectionView ItemCollectionView
        {
            get => collectionView;
            set
            {
                _ = SetProperty(ref collectionView, value);
                ItemCollectionView.SortDescriptions.Clear();
                ItemCollectionView.GroupDescriptions.Clear();
                ItemCollectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RevitElementModel.CategoryName)));
                ItemCollectionView.SortDescriptions.Add(new SortDescription(nameof(RevitElementModel.SymbolName), ListSortDirection.Ascending));
                ItemCollectionView.SortDescriptions.Add(new SortDescription(nameof(RevitElementModel.FamilyName), ListSortDirection.Ascending));
                ItemCollectionView.SortDescriptions.Add(new SortDescription(nameof(RevitElementModel.Description), ListSortDirection.Ascending));
            }
        }


        private ObservableCollection<RevitElementModel> elemList = new ObservableCollection<RevitElementModel>();
        public ObservableCollection<RevitElementModel> ElementList
        {
            get => elemList;
            set
            {
                _ = SetProperty(ref elemList, value);
                DockPanelView.CheckSelectAll.IsEnabled = elemList.Count != 0;
                ItemCollectionView = CollectionViewSource.GetDefaultView(value);
                IsEnabled = !ItemCollectionView.IsEmpty;
            }
        }
        #endregion


        #region TextFilter

        private string filterText = string.Empty;
        public string FilterText
        {
            get => filterText;
            set => SetProperty(ref filterText, value);
        }

        public ICommand SetFilterCommand { get; private set; }
        private void SetFilterTextCommand()
        {
            if (!ItemCollectionView.IsEmpty)
            {
                ItemCollectionView.Refresh();
                ItemCollectionView.Filter = FilterModelCollection;
            };
        }

        public ICommand CleanFilterCommand { get; private set; }
        private void СleanFilterTextCommand()
        {
            if (!ItemCollectionView.IsEmpty)
            {
                FilterText = string.Empty;
                ItemCollectionView.Refresh();
                ItemCollectionView.Filter = FilterModelCollection;
            };
        }

        private bool FilterModelCollection(object obj)
        {
            return string.IsNullOrEmpty(FilterText)
            || !(obj is RevitElementModel model) || model.SymbolName.Contains(FilterText)
            || model.SymbolName.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase)
            || model.FamilyName.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase)
            || model.CategoryName.Equals(FilterText, StringComparison.InvariantCultureIgnoreCase);
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


        #region SelectAllCommand
        public ICommand SelectAllCommand { get; private set; }
        public void HandleSelectAllCommand(bool? isChecked)
        {
            int num = 0;
            bool checkedHasValue = isChecked.HasValue;
            bool boolean = checkedHasValue && isChecked.Value;
            StringCollection collection = new StringCollection();
            if (ItemCollectionView != null)
            {
                try
                {
                    foreach (object item in ItemCollectionView)
                    {
                        if (item is RevitElementModel model)
                        {
                            if (checkedHasValue)
                            {
                                model.IsSelected = boolean;
                            }
                            if (model.IsSelected == true)
                            {
                                lock (collection.SyncRoot)
                                {
                                    _ = collection.Add(model.IdInt.ToString());
                                    num++;
                                }
                            }
                        }
                    }
                    Properties.Settings.Default.HostElementIdCollection = collection;
                    DockPanelView.CheckSelectAll.IsChecked = isChecked;
                    Properties.Settings.Default.Save();
                }
                catch (Exception exc)
                {
                    RevitLogger.Error(exc.Message);
                }
            }
        }

        #endregion


        #region SnoopCommand
        public ICommand SnoopCommand { get; private set; }
        private async Task ExecuteSnoopCommandAsync()
        {
            ElementList.Clear();
            ElementList = await RevitTask.RunAsync(app =>
            {
                DockPanelView.CheckSelectAll.IsChecked = false;
                CurrentDocument = app.ActiveUIDocument.Document;
                manager.InitializeActiveDocument(CurrentDocument);
                collection = manager.GetCollisionCommunicateElements();
                RevitLogger.Info($"Found collision {collection.Count()}");
                _ = ActivateFamilySimbol(rectangOpeningId);
                _ = ActivateFamilySimbol(roundOpeningId);
                return collection.ToObservableCollection();
            });
        }

        private bool ActivateFamilySimbol(int simbolIdInt)
        {
            bool result = false;
            if (0 < simbolIdInt)
            {
                Element element = CurrentDocument.GetElement(new ElementId(simbolIdInt));
                if (element is FamilySymbol symbol && !symbol.IsActive)
                {
                    try
                    {
                        symbol.Activate();
                        result = true;
                    }
                    catch
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        #endregion


        #region ApplyCommand
        public ICommand ApplyCommand { get; private set; }

        [STAThread]
        private async Task ExecuteApplyCommandAsync()
        {
            if (!openingView.IsActive)
            {

            }
            await RevitTask.RunAsync(app =>
            {
                CurrentDocument = app.ActiveUIDocument.Document;
                try
                {
                    if (true == openingView.ShowDialog())
                    {
                        _ = openingView.Activate();
                    }
                }
                catch (Exception ex)
                {
                    RevitLogger.Error(ex.Message);
                }
            });
        }

        #endregion


        public void Dispose()
        {
            manager?.Dispose();
            collection.Clear();
            ElementList.Clear();
            FilterText = string.Empty;
        }
    }
}