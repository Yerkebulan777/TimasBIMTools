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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Document = Autodesk.Revit.DB.Document;
using UserControl = System.Windows.Controls.UserControl;


namespace RevitTimasBIMTools.ViewModels
{
    public sealed class CutOpeningDataViewModel : ObservableObject, IDisposable
    {
        public CutOpeningDockPanelView DockPanelView { get; set; } = null;
        public static CancellationToken CancelToken { get; set; } = CancellationToken.None;

        private readonly object syncLocker = new object();
        private readonly ElementId elementId = ElementId.InvalidElementId;
        private IList<ElementModel> resultCollection = new List<ElementModel>(150);
        private readonly string roundOpeningId = Properties.Settings.Default.RoundSymbolUniqueId;
        private readonly string rectangOpeningId = Properties.Settings.Default.RectangSymbolUniqueId;
        private readonly CutOpeningCollisionManager manager = SmartToolController.Services.GetRequiredService<CutOpeningCollisionManager>();

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
                if (SetProperty(ref collectionView, value))
                {
                    ItemCollectionView.SortDescriptions.Clear();
                    ItemCollectionView.GroupDescriptions.Clear();
                    ItemCollectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.CategoryName)));
                    ItemCollectionView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.SymbolName), ListSortDirection.Ascending));
                    ItemCollectionView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.FamilyName), ListSortDirection.Ascending));
                    ItemCollectionView.SortDescriptions.Add(new SortDescription(nameof(ElementModel.Description), ListSortDirection.Ascending));
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
                    DockPanelView.CheckSelectAll.IsEnabled = modelCollection.Count != 0;
                    ItemCollectionView = CollectionViewSource.GetDefaultView(value);
                    UniqueElementModels = GetUniqueList(value);
                    IsEnabled = !ItemCollectionView.IsEmpty;
                }
            }
        }

        private IList<ElementModel> unique = null;
        public IList<ElementModel> UniqueElementModels
        {
            get => unique;
            set => SetProperty(ref unique, value);
        }


        private IList<ElementModel> GetUniqueList(Collection<ElementModel> collection)
        {
            return collection.GroupBy(i => i.SymbolName).Select(g => g.First()).OrderBy(i => i.FamilyName).Append(null).Distinct().ToList();
        }

        #endregion


        #region TextFilter

        private string filterText = string.Empty;
        private readonly IList<ElementModel> uniqueElementModels = null;

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
            || !(obj is ElementModel model) || model.SymbolName.Contains(FilterText)
            || model.SymbolName.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase)
            || model.FamilyName.StartsWith(FilterText, StringComparison.InvariantCultureIgnoreCase)
            || model.CategoryName.Equals(FilterText, StringComparison.InvariantCultureIgnoreCase);
        }

        private readonly ICollectionView view = CollectionViewSource.GetDefaultView(new ObservableCollection<ElementModel>());

        private void FindDuplicate(CollectionView collection)
        {
            if (!collection.IsEmpty)
            {
                collection.Refresh();
                //object value = collection.Select(product => product.ProductName).Distinct();
            };
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
            bool checkedHasValue = isChecked.HasValue;
            bool boolean = checkedHasValue && isChecked.Value;
            if (ItemCollectionView != null)
            {
                try
                {
                    foreach (object item in ItemCollectionView)
                    {
                        lock (syncLocker)
                        {
                            if (item is ElementModel model)
                            {
                                if (checkedHasValue)
                                {
                                    model.IsSelected = boolean;
                                }
                                if (model.IsSelected == true)
                                {
                                    resultCollection.Add(model);
                                }
                            }
                        }
                    }
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
            RevitElementModels.Clear();
            RevitElementModels = await RevitTask.RunAsync(app =>
            {
                DockPanelView.CheckSelectAll.IsChecked = false;
                CurrentDocument = app.ActiveUIDocument.Document;
                manager.InitializeActiveDocument(CurrentDocument);
                resultCollection = manager.GetCollisionCommunicateElements();
                RevitLogger.Info($"Found collision {resultCollection.Count()}");
                ActivateFamilySimbol(rectangOpeningId);
                ActivateFamilySimbol(roundOpeningId);
                return resultCollection.ToObservableCollection();
            });
        }

        private void ActivateFamilySimbol(string simbolId)
        {
            if (!string.IsNullOrEmpty(simbolId))
            {
                Element element = CurrentDocument.GetElement(simbolId);
                if (element is FamilySymbol symbol && !symbol.IsActive)
                {
                    symbol.Activate();
                }
            }
        }

        #endregion


        #region ApplyCommand
        public ICommand ApplyCommand { get; private set; }

        [STAThread]
        private async Task ExecuteApplyCommandAsync()
        {
            UserControl presenter = new UserControl
            {
                Height = 300,
                Width = 500
            };
            await RevitTask.RunAsync(app =>
            {
                CurrentDocument = app.ActiveUIDocument.Document;
                presenter.Content = GetContent(app.ActiveUIDocument);
            });
        }


        private ContentControl GetContent(UIDocument uidoc)
        {
            ContentControl content = null;
            Document document = uidoc.Document;
            View3D view3d = RevitViewManager.Get3dView(uidoc);
            try
            {
                Task.Delay(3000).Wait();
                ElementModel model = RevitElementModels.First();
                Element elem = document.GetElement(new ElementId(model.IdInt));
                if (RevitElementModels.Remove(model) && elem.IsValidObject)
                {
                    view3d = RevitViewManager.GetSectionBoxView(uidoc, elem, view3d);
                    content = new PreviewControl(document, view3d.Id);
                    RevitViewManager.SetColorElement(uidoc, elem);
                }
            }
            catch (Exception ex)
            {
                RevitLogger.Log(ex.Message);
            }
            return content;
        }
        #endregion


        public void Dispose()
        {
            manager?.Dispose();
            resultCollection.Clear();
            RevitElementModels.Clear();
            FilterText = string.Empty;
        }
    }
}